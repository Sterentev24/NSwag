//-----------------------------------------------------------------------
// <copyright file="TypeScriptOutputMode.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/RicoSuter/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

namespace NSwag.CodeGeneration.TypeScript
{
    /// <summary>The TypeScript client output mode.</summary>
    public enum TypeScriptOutputMode
    {
        /// <summary>Generate a single .ts file containing all clients, DTOs and utilities (default, backward-compatible).</summary>
        SingleFile,

        /// <summary>Generate a folder with one file per client class, one file per DTO, plus shared utilities and a barrel index.</summary>
        SplitByDto,
    }
}
