﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.Json.Schema
{
    /// <summary>
    /// Error numbers from reading a JSON schema.
    /// </summary>
    /// <remarks>
    /// Do not alter or reuse the integer values used in the enum initializers.
    /// They must remain stable because the error codes will be documented.
    /// </remarks>
    public enum ErrorNumber
    {
        /// <summary>
        /// No error.
        /// </summary>
        None = 0,

        #region Errors in schema document

        /// <summary>
        /// In the schema, a property that is required to be a string is not a string.
        /// </summary>
        /// <example>
        /// <code>
        /// {
        ///   "title": 2
        /// }
        /// </code>
        /// </example>
        NotAString = 1,

        /// <summary>
        /// In the schema, <code>additionalProperties</code> property is neither a Boolean
        /// nor an object.
        /// </summary>
        /// <example>
        /// <code>
        /// {
        ///   "additionalProperties": 2
        /// }
        /// </code>
        /// </example>
        InvalidAdditionalPropertiesType = 2,

        #endregion Errors in schema document

        #region Errors in instance document

        /// <summary>
        /// A token has the wrong type.
        /// </summary>
        /// <example>
        /// Schema:
        /// <code>
        /// {
        ///   "type": "integer"
        /// }
        /// </code>
        /// 
        /// Instance:
        /// <code>
        /// "x"
        /// </code>
        /// </example>
        WrongType = 1001,

        /// <summary>
        /// A required property is missing.
        /// </summary>
        /// <example>
        /// Schema:
        /// <code>
        /// {
        ///   "required": [ "a", "b", "c" ]
        /// }
        /// </code>
        /// 
        /// Instance:
        /// <code>
        /// {
        ///   "a": 1,
        ///   "b": 2
        /// }
        /// </code>
        /// </example>
        RequiredPropertyMissing = 1002,

        /// <summary>
        /// An array has too few items.
        /// </summary>
        /// <example>
        /// Schema:
        /// <code>
        /// {
        ///   "type": "array",
        ///   "minItems": 3
        /// }
        /// </code>
        /// 
        /// Instance:
        /// <code>
        /// [ 1, 2 ]
        /// </code>
        /// </example>
        TooFewArrayItems = 1003,

        /// <summary>
        /// An array has too many items.
        /// </summary>
        /// <example>
        /// Schema:
        /// <code>
        /// {
        ///   "type": "array",
        ///   "minItems": 3
        /// }
        /// </code>
        /// 
        /// Instance:
        /// <code>
        /// [ 1, 2, 3, 4 ]
        /// </code>
        /// </example>
        TooManyArrayItems = 1004,

        /// <summary>
        /// An object has a property not permitted by the schema.
        /// </summary>
        /// <example>
        /// Schema:
        /// <code>
        /// {
        /// }
        /// </code>
        /// 
        /// Instance:
        /// <code>
        /// {
        ///   "a": 3
        /// }
        /// </code>
        /// </example>
        AdditionalPropertiesProhibited = 1005,

        /// <summary>
        /// A numeric instance has a value greater than the maximum value permitted by the
        /// schema.
        /// </summary>
        /// <example>
        /// Schema:
        /// <code>
        /// {
        ///   "type": "integer",
        ///   "maximum": 2
        /// }
        /// </code>
        /// 
        /// Instance:
        /// <code>
        /// "3"
        /// </code>
        /// </example>
        ValueTooLarge = 1006,

        /// <summary>
        /// A numeric instance has a value greater than or equal to the exclusive maximum
        /// value permitted by the schema.
        /// </summary>
        /// <example>
        /// Schema:
        /// <code>
        /// {
        ///   "type": "integer",
        ///   "maximum": 2,
        ///   "exclusiveMaximum": true
        /// }
        /// </code>
        /// 
        /// Instance:
        /// <code>
        /// "2"
        /// </code>
        /// </example>
        ValueTooLargeExclusive = 1007,

        /// <summary>
        /// A numeric instance has a value less than the minimum value permitted by the
        /// schema.
        /// </summary>
        /// <example>
        /// Schema:
        /// <code>
        /// {
        ///   "type": "integer",
        ///   "minimum": 2
        /// }
        /// </code>
        /// 
        /// Instance:
        /// <code>
        /// "1"
        /// </code>
        /// </example>
        ValueTooSmall = 1008,

        /// <summary>
        /// A numeric instance has a value less than or equal to the exclusive minimum
        /// value permitted by the schema.
        /// </summary>
        /// <example>
        /// Schema:
        /// <code>
        /// {
        ///   "type": "integer",
        ///   "minimum": 2,
        ///   "exclusiveMinimum": true
        /// }
        /// </code>
        /// 
        /// Instance:
        /// <code>
        /// "2"
        /// </code>
        /// </example>
        ValueTooSmallExclusive = 1009,

        /// <summary>
        /// An object instance has more properties than the schema permits.
        /// </summary>
        /// <example>
        /// Schema:
        /// <code>
        /// {
        ///   "type": "object",
        ///   "maxProperties": 2,
        ///   "additionalProperties": true
        /// }
        /// </code>
        /// 
        /// Instance:
        /// <code>
        /// {
        ///   "a": 1,
        ///   "b": 2,
        ///   "c": 3
        /// }
        /// </code>
        /// </example>
        TooManyProperties = 1010,

        /// <summary>
        /// An object instance has fewer properties than the schema permits.
        /// </summary>
        /// <example>
        /// Schema:
        /// <code>
        /// {
        ///   "type": "object",
        ///   "minProperties": 2,
        ///   "additionalProperties": true
        /// }
        /// </code>
        /// 
        /// Instance:
        /// <code>
        /// {
        ///   "a": 1
        /// }
        /// </code>
        /// </example>
        TooFewProperties = 1011,


        /// <summary>
        /// An numeric instance is not a multiple of the specified value.
        /// </summary>
        /// <example>
        /// Schema:
        /// <code>
        /// {
        ///   "type": "number",
        ///   "multipleOf": 2
        /// }
        /// </code>
        /// 
        /// Instance:
        /// <code>
        /// "4.001"
        /// </code>
        /// </example>
        NotAMultiple = 1012,

        /// <summary>
        /// A string instance is longer than the schema permits.
        /// </summary>
        /// <example>
        /// Schema:
        /// <code>
        /// {
        ///   "type": "string",
        ///   "maxLength": 2
        /// }
        /// </code>
        /// 
        /// Instance:
        /// <code>
        /// "abc"
        /// </code>
        /// </example>
        StringTooLong = 1013,

        /// <summary>
        /// A string instance is shorter than the schema permits.
        /// </summary>
        /// <example>
        /// Schema:
        /// <code>
        /// {
        ///   "type": "string",
        ///   "minLength": 2
        /// }
        /// </code>
        /// 
        /// Instance:
        /// <code>
        /// "a"
        /// </code>
        /// </example>
        StringTooShort = 1014,

        /// <summary>
        /// A string instance does not match the required regular expression.
        /// </summary>
        /// <example>
        /// Schema:
        /// <code>
        /// {
        ///   "type": "string",
        ///   "pattern": "\\d{3}"
        /// }
        /// </code>
        /// 
        /// Instance:
        /// <code>
        /// "a12b"
        /// </code>
        /// </example>
        StringDoesNotMatchPattern = 1015,

        /// <summary>
        /// An instance does not match all of the schemas specified by "allOf".
        /// </summary>
        NotAllOf = 1016,

        /// <summary>
        /// An instance does not match any of the schemas specified by "anyOf".
        /// </summary>
        NotAnyOf = 1017,

        /// <summary>
        /// An instance matches either zero or more than one of the schemas
        /// specified by "oneOf".
        /// </summary>
        NotOneOf = 1018,

        /// <summary>
        /// An instance does not match any of the values specified by "enum".
        /// </summary>
        InvalidEnumValue = 1019

        #endregion Errors in instance document
    }
}
