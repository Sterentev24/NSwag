//-----------------------------------------------------------------------
// <copyright file="DtoPlacementPlanner.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using NJsonSchema.CodeGeneration;

namespace NSwag.CodeGeneration.TypeScript
{
    /// <summary>
    /// Builds the mapping between generated TypeScript type names and the relative file paths
    /// where they should live in split-by-DTO output mode.
    /// </summary>
    /// <remarks>
    /// Layout produced:
    /// <code>
    /// {outputRoot}/
    /// ├── index.ts
    /// ├── shared.ts       -- utilities (ApiException, FileResponse, FileParameter, throwException, BASE_URL token)
    /// ├── models/{TypeName}.ts    -- one DTO per file
    /// └── clients/{TypeName}.ts   -- one client class per file
    /// </code>
    /// </remarks>
    public sealed class DtoPlacementPlanner
    {
        /// <summary>Relative path (without extension) of the shared utilities module.</summary>
        public const string SharedModule = "shared";

        /// <summary>Relative path (without extension) of the barrel index module.</summary>
        public const string IndexModule = "index";

        /// <summary>Folder name for DTO files.</summary>
        public const string ModelsFolder = "models";

        /// <summary>Folder name for client files.</summary>
        public const string ClientsFolder = "clients";

        private readonly Dictionary<string, string> _typeToModule;
        private readonly HashSet<string> _sharedSymbols;

        /// <summary>Initializes the planner and pre-computes placement for all known symbols.</summary>
        /// <param name="dtoArtifacts">All DTO artifacts produced by the TypeScript generator.</param>
        /// <param name="clientArtifacts">All client class artifacts.</param>
        /// <param name="sharedSymbols">
        /// Names of symbols that must live in <c>shared.ts</c> (e.g. exception class name,
        /// <c>FileResponse</c>, <c>FileParameter</c>, response wrapper classes). Enumerated once.
        /// </param>
        public DtoPlacementPlanner(
            IEnumerable<CodeArtifact> dtoArtifacts,
            IEnumerable<CodeArtifact> clientArtifacts,
            IEnumerable<string> sharedSymbols)
        {
            if (dtoArtifacts == null) throw new ArgumentNullException(nameof(dtoArtifacts));
            if (clientArtifacts == null) throw new ArgumentNullException(nameof(clientArtifacts));
            if (sharedSymbols == null) throw new ArgumentNullException(nameof(sharedSymbols));

            _typeToModule = new Dictionary<string, string>(StringComparer.Ordinal);
            _sharedSymbols = new HashSet<string>(sharedSymbols, StringComparer.Ordinal);

            var caseInsensitiveGuard = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in _sharedSymbols)
            {
                Register(name, SharedModule, caseInsensitiveGuard);
            }

            foreach (var artifact in dtoArtifacts)
            {
                Register(artifact.TypeName, $"{ModelsFolder}/{artifact.TypeName}", caseInsensitiveGuard);
            }

            foreach (var artifact in clientArtifacts)
            {
                Register(artifact.TypeName, $"{ClientsFolder}/{artifact.TypeName}", caseInsensitiveGuard);
            }
        }

        /// <summary>
        /// Returns the relative module path (without extension) for a type,
        /// or <c>null</c> if the type is not owned by this planner (e.g. built-in TS types).
        /// </summary>
        /// <param name="typeName">The type name to look up.</param>
        public string GetModule(string typeName)
        {
            if (typeName == null) return null;
            return _typeToModule.TryGetValue(typeName, out var module) ? module : null;
        }

        /// <summary>Whether the given type name is one of the utility symbols that live in <c>shared.ts</c>.</summary>
        /// <param name="typeName">The type name.</param>
        public bool IsShared(string typeName)
        {
            return typeName != null && _sharedSymbols.Contains(typeName);
        }

        /// <summary>Enumerates all client modules (relative paths without extension).</summary>
        public IEnumerable<string> ClientModules =>
            _typeToModule.Where(kv => kv.Value.StartsWith(ClientsFolder + "/", StringComparison.Ordinal))
                         .Select(kv => kv.Value);

        /// <summary>Enumerates all DTO modules (relative paths without extension).</summary>
        public IEnumerable<string> DtoModules =>
            _typeToModule.Where(kv => kv.Value.StartsWith(ModelsFolder + "/", StringComparison.Ordinal))
                         .Select(kv => kv.Value);

        /// <summary>Enumerates all registered module paths (except the shared module).</summary>
        public IEnumerable<string> NonSharedModules =>
            _typeToModule.Values.Where(m => !string.Equals(m, SharedModule, StringComparison.Ordinal))
                                .Distinct(StringComparer.Ordinal);

        /// <summary>
        /// Computes the relative import path from a source module to a target module.
        /// Both arguments are module paths without extension, e.g. <c>"clients/PetClient"</c>.
        /// </summary>
        /// <param name="fromModule">The module that will contain the import statement.</param>
        /// <param name="toModule">The module being imported.</param>
        /// <returns>A path like <c>"./shared"</c>, <c>"../models/UserDto"</c>. Never uses <c>.ts</c> extension.</returns>
        public static string RelativePath(string fromModule, string toModule)
        {
            if (fromModule == null) throw new ArgumentNullException(nameof(fromModule));
            if (toModule == null) throw new ArgumentNullException(nameof(toModule));

            var fromParts = fromModule.Split('/');
            var toParts = toModule.Split('/');

            var commonPrefix = 0;
            var maxCommon = Math.Min(fromParts.Length - 1, toParts.Length - 1);
            while (commonPrefix < maxCommon &&
                   string.Equals(fromParts[commonPrefix], toParts[commonPrefix], StringComparison.Ordinal))
            {
                commonPrefix++;
            }

            var upSteps = fromParts.Length - 1 - commonPrefix;
            var downParts = toParts.Skip(commonPrefix).ToArray();

            if (upSteps == 0)
            {
                return "./" + string.Join("/", downParts);
            }

            return string.Concat(Enumerable.Repeat("../", upSteps)) + string.Join("/", downParts);
        }

        private void Register(string typeName, string module, Dictionary<string, string> caseInsensitiveGuard)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return;
            }

            if (_typeToModule.ContainsKey(typeName))
            {
                // Same symbol registered twice (e.g. shared + as DTO). Shared registration wins because it comes first.
                return;
            }

            if (caseInsensitiveGuard.TryGetValue(typeName, out var existing) &&
                !string.Equals(existing, typeName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Type name collision on case-insensitive filesystems: '{existing}' vs '{typeName}'. " +
                    "Rename one of them via NSwag settings or upstream OpenAPI schema.");
            }

            _typeToModule[typeName] = module;
            caseInsensitiveGuard[typeName] = typeName;
        }
    }
}
