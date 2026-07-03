using System.Linq;
using System.Threading.Tasks;
using NSwag.CodeGeneration.OperationNameGenerators;
using Xunit;

namespace NSwag.CodeGeneration.TypeScript.Tests
{
    public class SplitByDtoTests
    {
        private const string PetstoreLikeSpec = @"openapi: 3.0.0
info:
  version: '1.0.0'
  title: 'Split test API'
servers:
  - url: https://example.com/
paths:
  /pet/{id}:
    get:
      tags:
        - Pet
      operationId: getPet
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: integer
      responses:
        '200':
          description: OK
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Pet'
  /store/order:
    post:
      tags:
        - Store
      operationId: createOrder
      requestBody:
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/Order'
      responses:
        '200':
          description: OK
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Order'
components:
  schemas:
    Pet:
      type: object
      properties:
        id:
          type: integer
        name:
          type: string
        owner:
          $ref: '#/components/schemas/User'
    User:
      type: object
      properties:
        id:
          type: integer
        name:
          type: string
    Order:
      type: object
      properties:
        id:
          type: integer
        pet:
          $ref: '#/components/schemas/Pet'
";

        [Fact]
        public async Task Split_mode_produces_shared_index_and_per_type_files()
        {
            var document = await OpenApiYamlDocument.FromYamlAsync(PetstoreLikeSpec);
            var settings = new TypeScriptClientGeneratorSettings
            {
                Template = TypeScriptTemplate.Fetch,
                OutputMode = TypeScriptOutputMode.SplitByDto,
                OperationNameGenerator = new MultipleClientsFromFirstTagAndOperationNameGenerator(),
            };
            settings.TypeScriptGeneratorSettings.TypeStyle = NJsonSchema.CodeGeneration.TypeScript.TypeScriptTypeStyle.Interface;

            var generator = new TypeScriptClientGenerator(document, settings);
            var files = generator.GenerateFiles();

            Assert.Contains("shared.ts", files.Keys);
            Assert.Contains("index.ts", files.Keys);
            Assert.Contains("clients/PetClient.ts", files.Keys);
            Assert.Contains("clients/StoreClient.ts", files.Keys);
            Assert.Contains("models/Pet.ts", files.Keys);
            Assert.Contains("models/User.ts", files.Keys);
            Assert.Contains("models/Order.ts", files.Keys);
        }

        [Fact]
        public async Task Split_mode_imports_dtos_into_client_files()
        {
            var document = await OpenApiYamlDocument.FromYamlAsync(PetstoreLikeSpec);
            var settings = new TypeScriptClientGeneratorSettings
            {
                Template = TypeScriptTemplate.Fetch,
                OutputMode = TypeScriptOutputMode.SplitByDto,
                OperationNameGenerator = new MultipleClientsFromFirstTagAndOperationNameGenerator(),
            };
            settings.TypeScriptGeneratorSettings.TypeStyle = NJsonSchema.CodeGeneration.TypeScript.TypeScriptTypeStyle.Interface;

            var generator = new TypeScriptClientGenerator(document, settings);
            var files = generator.GenerateFiles();

            var petClient = files["clients/PetClient.ts"];
            Assert.Contains("import { Pet } from '../models/Pet';", petClient);

            var storeClient = files["clients/StoreClient.ts"];
            Assert.Contains("import { Order } from '../models/Order';", storeClient);
        }

        [Fact]
        public async Task Split_mode_imports_shared_exception_into_clients()
        {
            var document = await OpenApiYamlDocument.FromYamlAsync(PetstoreLikeSpec);
            var settings = new TypeScriptClientGeneratorSettings
            {
                Template = TypeScriptTemplate.Fetch,
                OutputMode = TypeScriptOutputMode.SplitByDto,
                ExceptionClass = "ApiException",
                OperationNameGenerator = new MultipleClientsFromFirstTagAndOperationNameGenerator(),
            };
            settings.TypeScriptGeneratorSettings.TypeStyle = NJsonSchema.CodeGeneration.TypeScript.TypeScriptTypeStyle.Interface;

            var generator = new TypeScriptClientGenerator(document, settings);
            var files = generator.GenerateFiles();

            var petClient = files["clients/PetClient.ts"];
            Assert.Contains("throwException", petClient);
            Assert.Matches(@"import\s*\{[^}]*throwException[^}]*\}\s*from\s*'\.\./shared';", petClient);

            var shared = files["shared.ts"];
            Assert.Contains("export class ApiException", shared);
            Assert.Contains("export function throwException", shared);
        }

        [Fact]
        public async Task Split_mode_imports_transitive_dto_into_dto_file()
        {
            var document = await OpenApiYamlDocument.FromYamlAsync(PetstoreLikeSpec);
            var settings = new TypeScriptClientGeneratorSettings
            {
                Template = TypeScriptTemplate.Fetch,
                OutputMode = TypeScriptOutputMode.SplitByDto,
                OperationNameGenerator = new MultipleClientsFromFirstTagAndOperationNameGenerator(),
            };
            settings.TypeScriptGeneratorSettings.TypeStyle = NJsonSchema.CodeGeneration.TypeScript.TypeScriptTypeStyle.Interface;

            var generator = new TypeScriptClientGenerator(document, settings);
            var files = generator.GenerateFiles();

            var petDto = files["models/Pet.ts"];
            Assert.Contains("import { User } from './User';", petDto);
        }

        [Fact]
        public async Task Split_mode_produces_index_barrel_with_all_modules()
        {
            var document = await OpenApiYamlDocument.FromYamlAsync(PetstoreLikeSpec);
            var settings = new TypeScriptClientGeneratorSettings
            {
                Template = TypeScriptTemplate.Fetch,
                OutputMode = TypeScriptOutputMode.SplitByDto,
                OperationNameGenerator = new MultipleClientsFromFirstTagAndOperationNameGenerator(),
            };
            settings.TypeScriptGeneratorSettings.TypeStyle = NJsonSchema.CodeGeneration.TypeScript.TypeScriptTypeStyle.Interface;

            var generator = new TypeScriptClientGenerator(document, settings);
            var files = generator.GenerateFiles();

            var index = files["index.ts"];
            Assert.Contains("export * from './shared';", index);
            Assert.Contains("export * from './clients/PetClient';", index);
            Assert.Contains("export * from './clients/StoreClient';", index);
            Assert.Contains("export * from './models/Pet';", index);
            Assert.Contains("export * from './models/User';", index);
            Assert.Contains("export * from './models/Order';", index);
        }

        [Fact]
        public async Task Split_mode_does_not_import_types_referenced_only_as_enum_members()
        {
            const string specWithEnum = @"openapi: 3.0.0
info: { title: 't', version: '1.0.0' }
paths:
  /pet:
    get:
      tags: [Pet]
      operationId: getPet
      responses:
        '200':
          description: OK
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Pet'
components:
  schemas:
    Pet:
      type: object
      properties:
        role:
          $ref: '#/components/schemas/PetRole'
    PetRole:
      type: string
      enum: [Pet, Owner]
";
            var document = await OpenApiYamlDocument.FromYamlAsync(specWithEnum);
            var settings = new TypeScriptClientGeneratorSettings
            {
                Template = TypeScriptTemplate.Fetch,
                OutputMode = TypeScriptOutputMode.SplitByDto,
                OperationNameGenerator = new MultipleClientsFromFirstTagAndOperationNameGenerator(),
            };
            settings.TypeScriptGeneratorSettings.TypeStyle = NJsonSchema.CodeGeneration.TypeScript.TypeScriptTypeStyle.Interface;

            var generator = new TypeScriptClientGenerator(document, settings);
            var files = generator.GenerateFiles();

            // The enum PetRole has a member literally named "Pet" — same as the Pet DTO.
            // Without the enum-member filter, PetRole.ts would import Pet from '../models/Pet' spuriously.
            var petRoleFile = files["models/PetRole.ts"];
            Assert.DoesNotContain("import { Pet }", petRoleFile);
        }

        [Fact]
        public async Task Single_file_mode_is_default_and_backward_compatible()
        {
            var document = await OpenApiYamlDocument.FromYamlAsync(PetstoreLikeSpec);
            var settings = new TypeScriptClientGeneratorSettings
            {
                Template = TypeScriptTemplate.Fetch,
                OperationNameGenerator = new MultipleClientsFromFirstTagAndOperationNameGenerator(),
            };
            settings.TypeScriptGeneratorSettings.TypeStyle = NJsonSchema.CodeGeneration.TypeScript.TypeScriptTypeStyle.Interface;

            Assert.Equal(TypeScriptOutputMode.SingleFile, settings.OutputMode);

            var generator = new TypeScriptClientGenerator(document, settings);
            var files = generator.GenerateFiles();

            Assert.Single(files);
            Assert.Contains(files.Keys, k => k == string.Empty);

            var single = files.First().Value;
            Assert.Contains("class PetClient", single);
            Assert.Contains("class StoreClient", single);
            Assert.Contains("interface Pet", single);
            Assert.Contains("interface Order", single);
        }
    }
}
