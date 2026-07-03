//-----------------------------------------------------------------------
// <copyright file="TypeScriptSplitFileTemplateModels.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace NSwag.CodeGeneration.TypeScript.Models
{
    /// <summary>Template model for a single client file (<c>clients/{ClientName}.ts</c>) in split mode.</summary>
    public class TypeScriptPerClientTemplateModel
    {
        private readonly TypeScriptFileTemplateModel _fileModel;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="fileModel">Existing full-file model (framework flags, exception class name, etc. are re-used).</param>
        /// <param name="clientCode">The already-rendered client class code (from <see cref="TypeScriptClientGenerator"/>).</param>
        /// <param name="importsBlock">The pre-computed <c>import</c> statement block for this file.</param>
        public TypeScriptPerClientTemplateModel(TypeScriptFileTemplateModel fileModel, string clientCode, string importsBlock)
        {
            _fileModel = fileModel;
            ClientCode = clientCode;
            ImportsBlock = importsBlock;
        }

        /// <summary>The rendered client class code (verbatim).</summary>
        public string ClientCode { get; }

        /// <summary>The import statement block (raw), prepended above the class code.</summary>
        public string ImportsBlock { get; }

        // The template forwards to File.Header which reads these members.
        // Delegate to the underlying file model so the same Liquid templates work.

        /// <inheritdoc cref="TypeScriptFileTemplateModel.Framework"/>
        public TypeScriptFrameworkModel Framework => _fileModel.Framework;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.GenerateClientClasses"/>
        public bool GenerateClientClasses => _fileModel.GenerateClientClasses;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.ImportRequiredTypes"/>
        public bool ImportRequiredTypes => _fileModel.ImportRequiredTypes;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.UseTransformOptionsMethod"/>
        public bool UseTransformOptionsMethod => _fileModel.UseTransformOptionsMethod;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.IncludeHttpContext"/>
        public bool IncludeHttpContext => _fileModel.IncludeHttpContext;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.SupportsTypeOnlyImports"/>
        public bool SupportsTypeOnlyImports => _fileModel.SupportsTypeOnlyImports;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.SupportsOverrideKeyword"/>
        public bool SupportsOverrideKeyword => _fileModel.SupportsOverrideKeyword;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.ExportTypes"/>
        public bool ExportTypes => _fileModel.ExportTypes;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.ExtensionCodeImport"/>
        public string ExtensionCodeImport => string.Empty; // extension code lives in shared.ts

        // Framework imports in File.Header are gated on this flag; per-client files never wrap in module/namespace.

        /// <summary>Always false — split mode is incompatible with module wrapping.</summary>
        public bool HasModuleName => false;

        /// <summary>Always false — split mode is incompatible with namespace wrapping.</summary>
        public bool HasNamespace => false;

        /// <summary>Whether MomentJS duration format is needed — false at client level (dates live in DTOs).</summary>
        public bool RequiresMomentJSDuration => false;
    }

    /// <summary>Template model for a single DTO file (<c>models/{TypeName}.ts</c>) in split mode.</summary>
    public class TypeScriptPerDtoTemplateModel
    {
        private readonly TypeScriptFileTemplateModel _fileModel;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="fileModel">Existing full-file model (used for framework flags).</param>
        /// <param name="dtoCode">The already-rendered DTO code.</param>
        /// <param name="importsBlock">The pre-computed <c>import</c> statement block for this file.</param>
        public TypeScriptPerDtoTemplateModel(TypeScriptFileTemplateModel fileModel, string dtoCode, string importsBlock)
        {
            _fileModel = fileModel;
            DtoCode = dtoCode;
            ImportsBlock = importsBlock;
        }

        /// <summary>The rendered DTO code (verbatim).</summary>
        public string DtoCode { get; }

        /// <summary>The import statement block (raw), prepended above the DTO code.</summary>
        public string ImportsBlock { get; }

        /// <inheritdoc cref="TypeScriptFileTemplateModel.Framework"/>
        public TypeScriptFrameworkModel Framework => _fileModel.Framework;

        /// <summary>Whether this specific DTO code uses <c>moment.duration(</c>.</summary>
        public bool RequiresMomentJSDuration => DtoCode.Contains("moment.duration(");

    }

    /// <summary>Template model for the shared utilities file (<c>shared.ts</c>) in split mode.</summary>
    public class TypeScriptSharedFileTemplateModel
    {
        private readonly TypeScriptFileTemplateModel _fileModel;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="fileModel">Existing full-file model (used verbatim for framework flags / exception class / response wrappers).</param>
        public TypeScriptSharedFileTemplateModel(TypeScriptFileTemplateModel fileModel)
        {
            _fileModel = fileModel;
        }

        /// <inheritdoc cref="TypeScriptFileTemplateModel.Framework"/>
        public TypeScriptFrameworkModel Framework => _fileModel.Framework;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.ExceptionClassName"/>
        public string ExceptionClassName => _fileModel.ExceptionClassName;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.RequiresExceptionClass"/>
        public bool RequiresExceptionClass => _fileModel.RequiresExceptionClass;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.RequiresFileResponseInterface"/>
        public bool RequiresFileResponseInterface => _fileModel.RequiresFileResponseInterface;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.RequiresFileParameterInterface"/>
        public bool RequiresFileParameterInterface => _fileModel.RequiresFileParameterInterface;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.WrapResponses"/>
        public bool WrapResponses => _fileModel.WrapResponses;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.GenerateResponseClasses"/>
        public bool GenerateResponseClasses => _fileModel.GenerateResponseClasses;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.ResponseClassNames"/>
        public IEnumerable<string> ResponseClassNames => _fileModel.ResponseClassNames;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.SupportsOverrideKeyword"/>
        public bool SupportsOverrideKeyword => _fileModel.SupportsOverrideKeyword;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.ExtensionCodeImport"/>
        public string ExtensionCodeImport => _fileModel.ExtensionCodeImport;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.ExtensionCodeTop"/>
        public string ExtensionCodeTop => _fileModel.ExtensionCodeTop;

        /// <inheritdoc cref="TypeScriptFileTemplateModel.ExtensionCodeBottom"/>
        public string ExtensionCodeBottom => _fileModel.ExtensionCodeBottom;

    }

    /// <summary>Template model for the barrel index file (<c>index.ts</c>) in split mode.</summary>
    public class TypeScriptIndexFileTemplateModel
    {
        /// <summary>Initializes a new instance with the list of module paths (without extension) to reexport.</summary>
        /// <param name="modulePaths">Relative paths like <c>./shared</c>, <c>./models/UserDto</c>, <c>./clients/PetClient</c>.</param>
        public TypeScriptIndexFileTemplateModel(IEnumerable<string> modulePaths)
        {
            Exports = modulePaths.ToArray();
        }

        /// <summary>Relative module paths to reexport (barrel entries).</summary>
        public IEnumerable<string> Exports { get; }
    }
}
