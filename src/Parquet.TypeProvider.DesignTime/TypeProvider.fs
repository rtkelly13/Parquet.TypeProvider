namespace Parquet.TypeProvider.DesignTime

open System
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open ProviderImplementation.ProvidedTypes
open Parquet.Schema
open Parquet.TypeProvider

[<TypeProvider>]
type ParquetTypeProvider(config: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(config, assemblyReplacementMap = [("Parquet.TypeProvider.DesignTime", "Parquet.TypeProvider.Runtime")])

    let asm = Assembly.GetExecutingAssembly()
    let ns = "Parquet.TypeProvider"

    let createTypes (typeName: string) (args: obj[]) =
        let samplePath = args.[0] :?> string
        let preferOption = args.[1] :?> bool

        let resolvedPath =
            if System.IO.Path.IsPathRooted(samplePath) then samplePath
            else System.IO.Path.Combine(config.ResolutionFolder, samplePath)

        if not (System.IO.File.Exists(resolvedPath)) then
            failwithf "Sample Parquet file '%s' was not found." resolvedPath

        // 1. Extract schema data fields from sample
        let fields = ParquetReaderCore.readSchemaFieldsFromFile(resolvedPath)

        // 2. Generate root provided type
        let generatedType = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>, isErased = true)

        // 3. Generate inner 'Row' type
        let rowType = ProvidedTypeDefinition("Row", Some typeof<ParquetRow>, isErased = true)
        rowType.AddXmlDoc("Represents a strongly-typed row in the Parquet dataset.")

        // Add properties to 'Row'
        fields
        |> Array.iteri (fun idx field ->
            let propType =
                if preferOption && field.IsNullable then
                    typedefof<option<_>>.MakeGenericType(field.ClrType)
                else
                    field.ClrType

            let prop =
                ProvidedProperty(
                    field.Name,
                    propType,
                    isStatic = false,
                    getterCode = (fun args ->
                        let rowInstance = args.[0]
                        if preferOption && field.IsNullable then
                            <@@ (%%rowInstance: ParquetRow).GetOptionalValue(idx) @@>
                        else
                            <@@ (%%rowInstance: ParquetRow).GetValue(idx) @@>
                    )
                )
            prop.AddXmlDoc($"Gets the {field.Name} column value.")
            rowType.AddMember(prop)
        )

        generatedType.AddMember(rowType)

        // 4. Generate static 'Load' method: filePath -> seq<Row>
        let loadMethod =
            ProvidedMethod(
                "Load",
                [ ProvidedParameter("filePath", typeof<string>) ],
                typedefof<seq<_>>.MakeGenericType(rowType),
                isStatic = true,
                invokeCode = (fun args ->
                    let filePathArg = args.[0]
                    <@@ ParquetReaderCore.loadFromFile (%%filePathArg: string) preferOption @@>
                )
            )
        loadMethod.AddXmlDoc("Loads and iterates rows from a Parquet file.")
        generatedType.AddMember(loadMethod)

        // 5. Generate static 'Load' method: stream -> seq<Row>
        let loadStreamMethod =
            ProvidedMethod(
                "Load",
                [ ProvidedParameter("stream", typeof<System.IO.Stream>) ],
                typedefof<seq<_>>.MakeGenericType(rowType),
                isStatic = true,
                invokeCode = (fun args ->
                    let streamArg = args.[0]
                    <@@ ParquetReaderCore.readRows (%%streamArg: System.IO.Stream) preferOption @@>
                )
            )
        loadStreamMethod.AddXmlDoc("Loads and iterates rows from a readable Parquet stream.")
        generatedType.AddMember(loadStreamMethod)

        // 6. Generate Columns container for direct columnar array access
        let columnsType = ProvidedTypeDefinition("Columns", Some typeof<obj>, isErased = true)
        columnsType.AddXmlDoc("Provides direct zero-allocation columnar array extractions.")

        fields
        |> Array.iter (fun field ->
            let colMethod =
                ProvidedMethod(
                    field.Name,
                    [ ProvidedParameter("filePath", typeof<string>) ],
                    field.ClrType.MakeArrayType(),
                    isStatic = true,
                    invokeCode = (fun args ->
                        let filePathArg = args.[0]
                        let fieldName = field.Name
                        <@@ ParquetReaderCore.readColumnArray (%%filePathArg: string) fieldName @@>
                    )
                )
            colMethod.AddXmlDoc($"Extracts the entire {field.Name} column as a contiguous typed array.")
            columnsType.AddMember(colMethod)
        )

        generatedType.AddMember(columnsType)

        generatedType

    let providerType = ProvidedTypeDefinition(asm, ns, "ParquetProvider", Some typeof<obj>, isErased = true)
    let staticParams =
        [
            ProvidedStaticParameter("Sample", typeof<string>)
            ProvidedStaticParameter("PreferOption", typeof<bool>, true)
        ]

    do
        providerType.DefineStaticParameters(staticParams, createTypes)
        providerType.AddXmlDoc("Provides strongly-typed schema inference for Apache Parquet files.")
        this.AddNamespace(ns, [ providerType ])

[<TypeProviderAssembly>]
do ()
