using System;
using System.Collections.Generic;
using System.Linq;
using NJsonSchema.CodeGeneration;
using Xunit;

namespace NSwag.CodeGeneration.TypeScript.Tests
{
    public class DtoPlacementPlannerTests
    {
        [Fact]
        public void GetModule_returns_models_path_for_dto()
        {
            var planner = Build(
                dtoNames: new[] { "UserDto", "PetDto" },
                clientNames: new[] { "PetClient" });

            Assert.Equal("models/UserDto", planner.GetModule("UserDto"));
            Assert.Equal("models/PetDto", planner.GetModule("PetDto"));
        }

        [Fact]
        public void GetModule_returns_clients_path_for_client()
        {
            var planner = Build(
                dtoNames: new[] { "UserDto" },
                clientNames: new[] { "PetClient", "StoreClient" });

            Assert.Equal("clients/PetClient", planner.GetModule("PetClient"));
            Assert.Equal("clients/StoreClient", planner.GetModule("StoreClient"));
        }

        [Fact]
        public void GetModule_returns_shared_for_utility_symbols()
        {
            var planner = Build(
                dtoNames: Array.Empty<string>(),
                clientNames: Array.Empty<string>(),
                sharedSymbols: new[] { "ApiException", "FileResponse", "FileParameter" });

            Assert.Equal("shared", planner.GetModule("ApiException"));
            Assert.Equal("shared", planner.GetModule("FileResponse"));
            Assert.Equal("shared", planner.GetModule("FileParameter"));
        }

        [Fact]
        public void GetModule_returns_null_for_unknown_type()
        {
            var planner = Build(
                dtoNames: new[] { "UserDto" },
                clientNames: new[] { "PetClient" });

            Assert.Null(planner.GetModule("SomethingElse"));
            Assert.Null(planner.GetModule("string"));
            Assert.Null(planner.GetModule(null));
        }

        [Fact]
        public void IsShared_reflects_shared_symbol_set()
        {
            var planner = Build(
                dtoNames: new[] { "UserDto" },
                clientNames: new[] { "PetClient" },
                sharedSymbols: new[] { "ApiException" });

            Assert.True(planner.IsShared("ApiException"));
            Assert.False(planner.IsShared("UserDto"));
            Assert.False(planner.IsShared("PetClient"));
            Assert.False(planner.IsShared("Unknown"));
        }

        [Fact]
        public void Shared_registration_wins_over_dto_registration_with_same_name()
        {
            var planner = Build(
                dtoNames: new[] { "ApiException" },
                clientNames: Array.Empty<string>(),
                sharedSymbols: new[] { "ApiException" });

            Assert.Equal("shared", planner.GetModule("ApiException"));
            Assert.True(planner.IsShared("ApiException"));
        }

        [Fact]
        public void Case_insensitive_collision_throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                Build(dtoNames: new[] { "UserDto", "userDto" }, clientNames: Array.Empty<string>()));

            Assert.Contains("case-insensitive", ex.Message);
        }

        [Fact]
        public void ClientModules_returns_only_clients()
        {
            var planner = Build(
                dtoNames: new[] { "UserDto", "PetDto" },
                clientNames: new[] { "PetClient", "StoreClient" });

            var modules = planner.ClientModules.ToArray();
            Assert.Contains("clients/PetClient", modules);
            Assert.Contains("clients/StoreClient", modules);
            Assert.DoesNotContain("models/UserDto", modules);
            Assert.DoesNotContain("shared", modules);
        }

        [Fact]
        public void DtoModules_returns_only_dtos()
        {
            var planner = Build(
                dtoNames: new[] { "UserDto", "PetDto" },
                clientNames: new[] { "PetClient" });

            var modules = planner.DtoModules.ToArray();
            Assert.Contains("models/UserDto", modules);
            Assert.Contains("models/PetDto", modules);
            Assert.DoesNotContain("clients/PetClient", modules);
        }

        [Theory]
        [InlineData("clients/PetClient", "models/UserDto", "../models/UserDto")]
        [InlineData("clients/PetClient", "shared", "../shared")]
        [InlineData("models/UserDto", "models/PetDto", "./PetDto")]
        [InlineData("models/UserDto", "shared", "../shared")]
        [InlineData("shared", "models/UserDto", "./models/UserDto")]
        [InlineData("index", "clients/PetClient", "./clients/PetClient")]
        [InlineData("index", "models/UserDto", "./models/UserDto")]
        [InlineData("index", "shared", "./shared")]
        public void RelativePath_computes_correct_relative_path(string from, string to, string expected)
        {
            Assert.Equal(expected, DtoPlacementPlanner.RelativePath(from, to));
        }

        private static DtoPlacementPlanner Build(
            IEnumerable<string> dtoNames,
            IEnumerable<string> clientNames,
            IEnumerable<string> sharedSymbols = null)
        {
            var dtos = dtoNames.Select(n => new CodeArtifact(
                n, CodeArtifactType.Class, CodeArtifactLanguage.TypeScript, CodeArtifactCategory.Contract, code: ""));
            var clients = clientNames.Select(n => new CodeArtifact(
                n, CodeArtifactType.Class, CodeArtifactLanguage.TypeScript, CodeArtifactCategory.Client, code: ""));
            return new DtoPlacementPlanner(dtos, clients, sharedSymbols ?? Array.Empty<string>());
        }
    }
}
