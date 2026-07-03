//-----------------------------------------------------------------------
// <copyright file="SwaggerToTypeScriptClientGenerator.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.TypeScript;
using NSwag.CodeGeneration.TypeScript.Models;

namespace NSwag.CodeGeneration.TypeScript
{
    /// <summary>Generates the CSharp service client code. </summary>
    public class TypeScriptClientGenerator : ClientGeneratorBase<TypeScriptOperationModel, TypeScriptParameterModel, TypeScriptResponseModel>
    {
        private readonly OpenApiDocument _document;
        private readonly TypeScriptTypeResolver _resolver;
        private readonly TypeScriptExtensionCode _extensionCode;

        /// <summary>Initializes a new instance of the <see cref="TypeScriptClientGenerator" /> class.</summary>
        /// <param name="document">The Swagger document.</param>
        /// <param name="settings">The settings.</param>
        /// <exception cref="ArgumentNullException"><paramref name="document" /> is <see langword="null" />.</exception>
        public TypeScriptClientGenerator(OpenApiDocument document, TypeScriptClientGeneratorSettings settings)
            : this(document, settings, new TypeScriptTypeResolver(settings.TypeScriptGeneratorSettings))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="TypeScriptClientGenerator" /> class.</summary>
        /// <param name="document">The Swagger document.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="resolver">The resolver.</param>
        /// <exception cref="ArgumentNullException"><paramref name="document" /> is <see langword="null" />.</exception>
        public TypeScriptClientGenerator(OpenApiDocument document, TypeScriptClientGeneratorSettings settings, TypeScriptTypeResolver resolver)
            : base(document, settings.CodeGeneratorSettings, resolver)
        {
            Settings = settings;

            _document = document ?? throw new ArgumentNullException(nameof(document));
            _resolver = resolver;
            _resolver.RegisterSchemaDefinitions(_document.Definitions);

            _extensionCode = new TypeScriptExtensionCode(
                Settings.TypeScriptGeneratorSettings.ExtensionCode,
                (Settings.TypeScriptGeneratorSettings.ExtendedClasses ?? new string[] { }).Concat(new[] { Settings.ConfigurationClass }).ToArray(),
                new[] { Settings.ClientBaseClass });
        }

        /// <summary>Gets or sets the generator settings.</summary>
        public TypeScriptClientGeneratorSettings Settings { get; set; }

        /// <summary>Gets the base settings.</summary>
        public override ClientGeneratorBaseSettings BaseSettings => Settings;

        /// <summary>Gets the type.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">Specifies whether the type is nullable..</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        public override string GetTypeName(JsonSchema schema, bool isNullable, string typeNameHint)
        {
            if (schema == null)
            {
                return "void";
            }

            if (schema.ActualTypeSchema.IsBinary)
            {
                return GetBinaryResponseTypeName();
            }

            return _resolver.Resolve(schema.ActualSchema, isNullable, typeNameHint);
        }

        /// <summary>Gets the file response type name.</summary>
        /// <returns>The type name.</returns>
        public override string GetBinaryResponseTypeName()
        {
            return Settings.Template != TypeScriptTemplate.JQueryCallbacks &&
                   Settings.Template != TypeScriptTemplate.JQueryPromises ? "FileResponse" : "any";
        }

        /// <summary>Generates the file.</summary>
        /// <param name="clientTypes">The client types.</param>
        /// <param name="dtoTypes">The DTO types.</param>
        /// <param name="outputType">Type of the output.</param>
        /// <returns>The code.</returns>
        protected override string GenerateFile(IEnumerable<CodeArtifact> clientTypes, IEnumerable<CodeArtifact> dtoTypes, ClientGeneratorOutputType outputType)
        {
            var model = new TypeScriptFileTemplateModel(clientTypes, dtoTypes, _document, _extensionCode, Settings, _resolver);
            var template = BaseSettings.CodeGeneratorSettings.TemplateFactory.CreateTemplate("TypeScript", "File", model);
            return template.Render();
        }

        /// <summary>
        /// Generates the output as a collection of files. In <see cref="TypeScriptOutputMode.SingleFile"/>
        /// mode returns one entry with the key <c>""</c> (empty). In <see cref="TypeScriptOutputMode.SplitByDto"/>
        /// mode returns one file per client, one file per DTO, plus <c>shared.ts</c> and <c>index.ts</c>.
        /// </summary>
        public override IDictionary<string, string> GenerateFiles()
        {
            if (Settings.OutputMode == TypeScriptOutputMode.SingleFile)
            {
                return base.GenerateFiles();
            }

            var tsSettings = Settings.TypeScriptGeneratorSettings;
            if (!string.IsNullOrEmpty(tsSettings.ModuleName))
            {
                throw new InvalidOperationException(
                    $"OutputMode '{Settings.OutputMode}' is incompatible with ModuleName ('{tsSettings.ModuleName}'). " +
                    "Split output produces ES modules; module wrapping is not supported.");
            }
            if (!string.IsNullOrEmpty(tsSettings.Namespace))
            {
                throw new InvalidOperationException(
                    $"OutputMode '{Settings.OutputMode}' is incompatible with Namespace ('{tsSettings.Namespace}'). " +
                    "Split output produces ES modules; namespace wrapping is not supported.");
            }

            return GenerateFilesSplitByDto();
        }

        private IDictionary<string, string> GenerateFilesSplitByDto()
        {
            var clientArtifacts = base.GenerateAllClientTypes().ToList();
            var dtoArtifacts = Settings.GenerateDtoTypes
                ? GenerateDtoTypes().ToList()
                : new List<CodeArtifact>();

            var fileModel = new TypeScriptFileTemplateModel(
                clientArtifacts, dtoArtifacts, _document, _extensionCode, Settings, _resolver);

            var sharedSymbols = CollectSharedSymbols(fileModel);
            var planner = new DtoPlacementPlanner(dtoArtifacts, clientArtifacts, sharedSymbols);

            var files = new Dictionary<string, string>(StringComparer.Ordinal);
            var templateFactory = Settings.CodeGeneratorSettings.TemplateFactory;

            var sharedModel = new TypeScriptSharedFileTemplateModel(fileModel);
            files[DtoPlacementPlanner.SharedModule + ".ts"] =
                Render(templateFactory.CreateTemplate("TypeScript", "File.Shared", sharedModel));

            foreach (var clientArtifact in clientArtifacts)
            {
                var module = planner.GetModule(clientArtifact.TypeName);
                if (module == null) continue;

                var imports = BuildImportsBlock(clientArtifact.TypeName, clientArtifact.Code, module, planner);
                var perClientModel = new TypeScriptPerClientTemplateModel(fileModel, clientArtifact.Code, imports);
                files[module + ".ts"] =
                    Render(templateFactory.CreateTemplate("TypeScript", "File.PerClient", perClientModel));
            }

            foreach (var dtoArtifact in dtoArtifacts)
            {
                var module = planner.GetModule(dtoArtifact.TypeName);
                if (module == null) continue;

                var imports = BuildImportsBlock(dtoArtifact.TypeName, dtoArtifact.Code, module, planner);
                var perDtoModel = new TypeScriptPerDtoTemplateModel(fileModel, dtoArtifact.Code, imports);
                files[module + ".ts"] =
                    Render(templateFactory.CreateTemplate("TypeScript", "File.PerDto", perDtoModel));
            }

            var indexEntries = new List<string> { "./" + DtoPlacementPlanner.SharedModule };
            indexEntries.AddRange(planner.NonSharedModules.OrderBy(m => m, StringComparer.Ordinal).Select(m => "./" + m));
            var indexModel = new TypeScriptIndexFileTemplateModel(indexEntries);
            files[DtoPlacementPlanner.IndexModule + ".ts"] =
                Render(templateFactory.CreateTemplate("TypeScript", "File.Index", indexModel));

            return files;
        }

        private static string Render(NJsonSchema.CodeGeneration.ITemplate template)
        {
            return template.Render()
                .Replace("\r", string.Empty)
                .Replace("\n\n\n\n", "\n\n")
                .Replace("\n\n\n", "\n\n");
        }

        /// <summary>
        /// Determines which type names must live in <c>shared.ts</c> in split mode.
        /// Includes the exception class, response wrappers, <c>FileResponse</c>, <c>FileParameter</c>
        /// and Angular BASE_URL token (when applicable).
        /// </summary>
        private static IEnumerable<string> CollectSharedSymbols(TypeScriptFileTemplateModel fileModel)
        {
            if (fileModel.RequiresExceptionClass)
            {
                yield return fileModel.ExceptionClassName;
            }

            if (fileModel.RequiresFileResponseInterface)
            {
                yield return "FileResponse";
            }

            if (fileModel.RequiresFileParameterInterface)
            {
                yield return "FileParameter";
            }

            if (fileModel.WrapResponses && fileModel.GenerateResponseClasses)
            {
                foreach (var responseClass in fileModel.ResponseClassNames)
                {
                    yield return responseClass;
                }
            }

            if (fileModel.Framework.IsAngular)
            {
                yield return fileModel.Framework.Angular.BaseUrlTokenName;
                yield return "throwException";
            }
            else if (fileModel.RequiresExceptionClass)
            {
                // Non-Angular templates also emit throwException as a top-level helper.
                yield return "throwException";
            }
        }

        // Regex identifying whole-word identifier occurrences (ASCII-only — NSwag never emits Unicode identifiers).
        // The negative lookahead skips identifiers used as an enum member's left-hand-side (e.g. `User = "user"`),
        // where the name is a member label, not a type reference. Without this filter, an enum whose member name
        // happens to match another registered type name would gain a spurious import.
        private static readonly Regex IdentifierScan =
            new Regex(@"\b[A-Za-z_][A-Za-z0-9_]*\b(?!\s*=\s*[""'0-9])", RegexOptions.Compiled);

        // Matches double-quoted, single-quoted and back-tick string literals so we can strip their bodies
        // before scanning for identifiers. Enum values, JSON keys, method routes etc. often contain words
        // that happen to match another registered type name and would otherwise create spurious imports.
        private static readonly Regex StringLiteralScan =
            new Regex(@"""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'|`(?:\\.|[^`\\])*`", RegexOptions.Compiled);

        /// <summary>
        /// Builds the import statement block for a single split file by scanning its code for known symbol
        /// occurrences, resolving each through the placement planner, and emitting one <c>import</c> per
        /// target module.
        /// </summary>
        private static string BuildImportsBlock(string ownTypeName, string code, string ownModule, DtoPlacementPlanner planner)
        {
            // Blank out the interior of every string literal so identifiers used only as string content
            // (JSON keys, enum values, HTTP method routes) do not produce spurious imports.
            var scannable = StringLiteralScan.Replace(code, m =>
            {
                var quote = m.Value[0];
                return quote + new string(' ', m.Length - 2) + quote;
            });

            var referenced = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in IdentifierScan.Matches(scannable))
            {
                var name = match.Value;
                if (name == ownTypeName) continue;
                if (planner.GetModule(name) == null) continue;
                referenced.Add(name);
            }

            if (referenced.Count == 0) return string.Empty;

            var byModule = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
            foreach (var name in referenced)
            {
                var targetModule = planner.GetModule(name);
                if (targetModule == ownModule) continue;

                if (!byModule.TryGetValue(targetModule, out var symbols))
                {
                    symbols = new SortedSet<string>(StringComparer.Ordinal);
                    byModule[targetModule] = symbols;
                }
                symbols.Add(name);
            }

            if (byModule.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            foreach (var kv in byModule)
            {
                var relative = DtoPlacementPlanner.RelativePath(ownModule, kv.Key);
                sb.Append("import { ")
                  .Append(string.Join(", ", kv.Value))
                  .Append(" } from '")
                  .Append(relative)
                  .Append("';\n");
            }
            sb.Append('\n');
            return sb.ToString();
        }

        /// <summary>Generates the client class.</summary>
        /// <param name="controllerName">Name of the controller.</param>
        /// <param name="controllerClassName">Name of the controller class.</param>
        /// <param name="operations">The operations.</param>
        /// <returns>The code.</returns>
        protected override IEnumerable<CodeArtifact> GenerateClientTypes(string controllerName, string controllerClassName, IEnumerable<TypeScriptOperationModel> operations)
        {
            UpdateUseDtoClassAndDataConversionCodeProperties(operations);

            var model = new TypeScriptClientTemplateModel(controllerName, controllerClassName, operations, _extensionCode, _document, Settings);
            var template = Settings.CreateTemplate(model);
            yield return new CodeArtifact(model.Class, CodeArtifactType.Class, CodeArtifactLanguage.CSharp, CodeArtifactCategory.Client, template);
        }

        /// <summary>Generates all DTO types.</summary>
        /// <returns>The code artifact collection.</returns>
        protected override IEnumerable<CodeArtifact> GenerateDtoTypes()
        {
            var generator = new TypeScriptGenerator(_document, Settings.TypeScriptGeneratorSettings, _resolver);
            return generator.GenerateTypes(_extensionCode);
        }

        /// <summary>Creates an operation model.</summary>
        /// <param name="operation"></param>
        /// <param name="settings">The settings.</param>
        /// <returns>The operation model.</returns>
        protected override TypeScriptOperationModel CreateOperationModel(OpenApiOperation operation, ClientGeneratorBaseSettings settings)
        {
            return new TypeScriptOperationModel(operation, (TypeScriptClientGeneratorSettings)settings, this, Resolver);
        }

        private void UpdateUseDtoClassAndDataConversionCodeProperties(IEnumerable<TypeScriptOperationModel> operations)
        {
            // TODO: Remove this method => move to appropriate location

            foreach (var operation in operations)
            {
                foreach (var response in operation.Responses.Where(r => r.HasType))
                {
                    response.DataConversionCode = DataConversionGenerator.RenderConvertToClassCode(new DataConversionParameters
                    {
                        Variable = "result" + response.StatusCode,
                        Value = "resultData" + response.StatusCode,
                        Schema = response.ResolvableResponseSchema,
                        CheckNewableObject = response.IsNullable,
                        IsPropertyNullable = response.IsNullable,
                        TypeNameHint = string.Empty,
                        Settings = Settings.TypeScriptGeneratorSettings,
                        Resolver = _resolver,
                        NullValue = TypeScriptNullValue.Null
                    });
                }

                if (operation.HasDefaultResponse && operation.DefaultResponse.HasType)
                {
                    operation.DefaultResponse.DataConversionCode = DataConversionGenerator.RenderConvertToClassCode(new DataConversionParameters
                    {
                        Variable = "result" + operation.DefaultResponse.StatusCode,
                        Value = "resultData" + operation.DefaultResponse.StatusCode,
                        Schema = operation.DefaultResponse.ResolvableResponseSchema,
                        CheckNewableObject = operation.DefaultResponse.IsNullable,
                        IsPropertyNullable = operation.DefaultResponse.IsNullable,
                        TypeNameHint = string.Empty,
                        Settings = Settings.TypeScriptGeneratorSettings,
                        Resolver = _resolver,
                        NullValue = TypeScriptNullValue.Null
                    });
                }
            }
        }
    }
}
