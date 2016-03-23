﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Json.Schema.UnitTests;
using Xunit;

namespace Microsoft.Json.Schema.ToDotNet.UnitTests
{
    public class DataModelGeneratorTests
    {
        private const string PrimaryOutputFilePath = TestFileSystem.OutputDirectory + "\\" + TestSettings.RootClassName + ".cs";

        private TestFileSystem _testFileSystem;
        private readonly DataModelGeneratorSettings _settings;

        public DataModelGeneratorTests()
        {
            _testFileSystem = new TestFileSystem();
            _settings = TestSettings.MakeSettings();
        }

        [Fact(DisplayName = "DataModelGenerator throws if output directory exists")]
        public void ThrowsIfOutputDirectoryExists()
        {
            _settings.ForceOverwrite = false;

            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            Action action = () => generator.Generate(new JsonSchema());

            // ... and the message should mention the output directory.
            action.ShouldThrow<JSchemaException>().WithMessage($"*{TestFileSystem.OutputDirectory}*");
        }

        [Fact(DisplayName = "DataModelGenerator does not throw if output directory does not exist")]
        public void DoesNotThrowIfOutputDirectoryDoesNotExist()
        {
            // Use a directory name other than the default. The mock file system believes
            // that only the default directory exists.
            _settings.OutputDirectory = _settings.OutputDirectory + "x";

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("Basic");

            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            Action action = () => generator.Generate(schema);

            action.ShouldNotThrow();
        }

        [Fact(DisplayName = "DataModelGenerator does not throw if ForceOverwrite setting is set")]
        public void DoesNotThowIfForceOverwriteSettingIsSet()
        {
            // This is the default from MakeSettings; restated here for explicitness.
            _settings.ForceOverwrite = true;

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("Basic");

            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            Action action = () => generator.Generate(schema);

            action.ShouldNotThrow();
        }

        [Fact(DisplayName = "DataModelGenerator throws if root schema is not of type 'object'")]
        public void ThrowsIfRootSchemaIsNotOfTypeObject()
        {
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("NotAnObject");

            Action action = () => generator.Generate(schema);

            // ... and the message should mention what the root type actually was.
            action.ShouldThrow<JSchemaException>().WithMessage("*number*");
        }

        [Fact(DisplayName = "DataModelGenerator generates class description")]
        public void GeneratesClassDescription()
        {
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("Basic");

            string actual = generator.Generate(schema);

            const string Expected =
@"using System;

namespace N
{
    /// <summary>
    /// The description
    /// </summary>
    public partial class C : IEquatable<C>
    {
    }
}";
            actual.Should().Be(Expected);

            // This particular test shows not only that the correct text was produced,
            // but that it was written to the expected path. Subsequent tests will
            // just verify the text.
            _testFileSystem.Files.Should().OnlyContain(key => key.Equals(PrimaryOutputFilePath));
        }

        [Fact(DisplayName = "DataModelGenerator generates properties with built-in types")]
        public void GeneratesPropertiesWithBuiltInTypes()
        {
            _settings.GenerateOverrides = true;
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("Properties");

            string actual = generator.Generate(schema);

            const string Expected =
@"using System;

namespace N
{
    public partial class C : IEquatable<C>
    {
        public string StringProp { get; set; }
        public double NumberProp { get; set; }
        public bool BooleanProp { get; set; }
        public int IntegerProp { get; set; }

        public override bool Equals(object other)
        {
            return Equals(other as C);
        }

        public override int GetHashCode()
        {
            int result = 17;
            unchecked
            {
                if (StringProp != null)
                {
                    result = (result * 31) + StringProp.GetHashCode();
                }

                result = (result * 31) + NumberProp.GetHashCode();
                result = (result * 31) + BooleanProp.GetHashCode();
                result = (result * 31) + IntegerProp.GetHashCode();
            }

            return result;
        }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }

            if (StringProp != other.StringProp)
            {
                return false;
            }

            if (NumberProp != other.NumberProp)
            {
                return false;
            }

            if (BooleanProp != other.BooleanProp)
            {
                return false;
            }

            if (IntegerProp != other.IntegerProp)
            {
                return false;
            }

            return true;
        }
    }
}";
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates object-valued property with correct type")]
        public void GeneratesObjectValuedPropertyWithCorrectType()
        {
            _settings.GenerateOverrides = true;
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("Object");

            const string Expected =
@"using System;

namespace N
{
    public partial class C : IEquatable<C>
    {
        public D ObjectProp { get; set; }

        public override bool Equals(object other)
        {
            return Equals(other as C);
        }

        public override int GetHashCode()
        {
            int result = 17;
            unchecked
            {
                if (ObjectProp != null)
                {
                    result = (result * 31) + ObjectProp.GetHashCode();
                }
            }

            return result;
        }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }

            if (!Object.Equals(ObjectProp, other.ObjectProp))
            {
                return false;
            }

            return true;
        }
    }
}";
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator throws if reference is not a fragment")]
        public void ThrowsIfReferenceIsNotAFragment()
        {
            const string SchemaText = @"
{
  ""type"": ""object"",
  ""properties"": {
    ""p"": {
      ""$ref"": ""https://example.com/pschema.schema.json/#""
    }
  },
}";
            JsonSchema schema = SchemaReader.ReadSchema(SchemaText);
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            Action action = () => generator.Generate(schema);

            action.ShouldThrow<JSchemaException>()
                .WithMessage("*https://example.com/pschema.schema.json/#*");
        }

        [Fact(DisplayName = "DataModelGenerator throws if reference does not specify a definition")]
        public void ThrowsIfReferenceDoesNotSpecifyADefinition()
        {
            const string SchemaText = @"
{
  ""type"": ""object"",
  ""properties"": {
    ""p"": {
      ""$ref"": ""#/notDefinitions/p""
    }
  },
  ""notDefinitions"": {
    ""p"": {
    }
  }
}";
            JsonSchema schema = SchemaReader.ReadSchema(SchemaText);
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            Action action = () => generator.Generate(schema);

            action.ShouldThrow<JSchemaException>()
                .WithMessage("*#/notDefinitions/p*");
        }

        [Fact(DisplayName = "Throws if referenced definition does not exist")]
        public void ThrowsIfReferencedDefinitionDoesNotExist()
        {
            const string SchemaText = @"
{
  ""type"": ""object"",
  ""properties"": {
    ""p"": {
      ""$ref"": ""#/definitions/nonExistentDefinition""
    }
  },
  ""definitions"": {
    ""p"": {
    }
  }
}";
            JsonSchema schema = SchemaReader.ReadSchema(SchemaText);
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            Action action = () => generator.Generate(schema);

            action.ShouldThrow<JSchemaException>()
                .WithMessage("*nonExistentDefinition*");
        }

        [Fact(DisplayName = "DataModelGenerator generates array-valued property")]
        public void GeneratesArrayValuedProperty()
        {
            _settings.GenerateOverrides = true;
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("Array");

            const string Expected =
@"using System;
using System.Collections.Generic;

namespace N
{
    public partial class C : IEquatable<C>
    {
        public IList<object> ArrayProp { get; set; }

        public override bool Equals(object other)
        {
            return Equals(other as C);
        }

        public override int GetHashCode()
        {
            int result = 17;
            unchecked
            {
                if (ArrayProp != null)
                {
                    foreach (var value_0 in ArrayProp)
                    {
                        result = result * 31;
                        if (value_0 != null)
                        {
                            result = (result * 31) + value_0.GetHashCode();
                        }
                    }
                }
            }

            return result;
        }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }

            if (!Object.ReferenceEquals(ArrayProp, other.ArrayProp))
            {
                if (ArrayProp == null || other.ArrayProp == null)
                {
                    return false;
                }

                if (ArrayProp.Count != other.ArrayProp.Count)
                {
                    return false;
                }

                for (int value_0 = 0; value_0 < ArrayProp.Count; ++value_0)
                {
                    if (!Object.Equals(ArrayProp[value_0], other.ArrayProp[value_0]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}";
            string actual = generator.Generate(schema);

            TestUtil.WriteTestResultFiles(Expected, actual, nameof(GeneratesArrayValuedProperty));

            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates XML comments for properties")]
        public void GeneratesXmlCommentsForProperties()
        {
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("PropertyDescription");

            const string Expected =
@"using System;

namespace N
{
    public partial class C : IEquatable<C>
    {
        /// <summary>
        /// An example property.
        /// </summary>
        public string ExampleProp { get; set; }
    }
}";

            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates XML comments for properties whose property type is ref")]
        public void GeneratesXmlCommentsForPropertiesWhosePropertyTypeIsRef()
        {
            _settings.RootClassName = "consoleWindow";
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""description"": ""Describes a console window."",
  ""properties"": {
    ""foregroundColor"": {
      ""$ref"": ""#/definitions/color"",
      ""description"": ""The color of the text on the screen.""
    },
    ""backgroundColor"": {
      ""$ref"": ""#/definitions/color"",
      ""description"": ""The color of the screen background.""
    },
  },
  ""definitions"": {
    ""color"": {
      ""type"": ""object"",
      ""description"": ""Describes a color with R, G, and B components."",
      ""properties"": {
        ""red"": {
          ""type"": ""integer"",
          ""description"": ""The value of the R component.""
        },
        ""green"": {
          ""type"": ""integer"",
          ""description"": ""The value of the G component.""
        },
        ""blue"": {
          ""type"": ""integer"",
          ""description"": ""The value of the B component.""
        }
      }
    }
  }
}");

            const string RootClassText =
@"using System;

namespace N
{
    /// <summary>
    /// Describes a console window.
    /// </summary>
    public partial class ConsoleWindow : IEquatable<ConsoleWindow>
    {
        /// <summary>
        /// The color of the text on the screen.
        /// </summary>
        public Color ForegroundColor { get; set; }

        /// <summary>
        /// The color of the screen background.
        /// </summary>
        public Color BackgroundColor { get; set; }
    }
}";

            const string ColorClassText =
@"using System;

namespace N
{
    /// <summary>
    /// Describes a color with R, G, and B components.
    /// </summary>
    public partial class Color : IEquatable<Color>
    {
        /// <summary>
        /// The value of the R component.
        /// </summary>
        public int Red { get; set; }

        /// <summary>
        /// The value of the G component.
        /// </summary>
        public int Green { get; set; }

        /// <summary>
        /// The value of the B component.
        /// </summary>
        public int Blue { get; set; }
    }
}";

            generator.Generate(schema);

            string rootFilePath = TestFileSystem.MakeOutputFilePath(_settings.RootClassName.ToPascalCase());
            string colorFilePath = TestFileSystem.MakeOutputFilePath("Color");

            var expectedOutputFiles = new List<string>
            {
                rootFilePath,
                colorFilePath
            };

            _testFileSystem.Files.Count.Should().Be(expectedOutputFiles.Count);
            _testFileSystem.Files.Should().OnlyContain(key => expectedOutputFiles.Contains(key));

            _testFileSystem[rootFilePath].Should().Be(RootClassText);
            _testFileSystem[colorFilePath].Should().Be(ColorClassText);
        }

        [Fact(DisplayName = "DataModelGenerator generates copyright notice")]
        public void GeneratesCopyrightAtTopOfFile()
        {
            _settings.CopyrightNotice =
@"// Copyright (c) 2016. All rights reserved.
// Licensed under Apache 2.0 license.

";
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("PropertyDescription");

            const string Expected =
@"// Copyright (c) 2016. All rights reserved.
// Licensed under Apache 2.0 license.

using System;

namespace N
{
    public partial class C : IEquatable<C>
    {
        /// <summary>
        /// An example property.
        /// </summary>
        public string ExampleProp { get; set; }
    }
}";
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates cloning code")]
        public void GeneratesCloningCode()
        {
            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""properties"": {
    ""intProp"": {
      ""type"": ""integer"",
      ""description"": ""An integer property.""
    },
    ""stringProp"": {
      ""type"": ""string"",
      ""description"": ""A string property.""
    },
    ""arrayProp"": {
      ""type"": ""array"",
      ""description"": ""An array property."",
      ""items"": {
        ""type"": ""number""
      }
    },
    ""uriProp"": {
      ""type"": ""string"",
      ""description"": ""A Uri property."",
      ""format"": ""uri""
    },
    ""dateTimeProp"": {
      ""type"": ""string"",
      ""description"": ""A DateTime property."",
      ""format"": ""date-time""
    },
    ""referencedTypeProp"": {
      ""$ref"": ""#/definitions/d""
    },
    ""arrayOfRefProp"": {
      ""type"": ""array"",
      ""description"": ""An array of a cloneable type."",
      ""items"": {
        ""$ref"": ""#/definitions/d""
      }
    },
    ""arrayOfArrayProp"": {
      ""type"": ""array"",
      ""description"": ""An array of arrays."",
      ""items"": {
        ""type"": ""array"",
        ""items"": {
          ""$ref"": ""#/definitions/d""
        }
      }
    },
    ""dictionaryProp"": {
      ""description"": ""A dictionary property."",
      ""type"": ""object""
    }
  },
  ""definitions"": {
    ""d"": {
      ""type"": ""object""
    }
  }
}");

            const string HintsText =
@"{
  ""C.DictionaryProp"": [
    {
      ""$type"": ""Microsoft.Json.Schema.ToDotNet.DictionaryHint, Microsoft.Json.Schema.ToDotNet""
    }
  ]
}";

            const string ExpectedClass =
@"using System;
using System.Collections.Generic;

namespace N
{
    public partial class C : ISyntax, IEquatable<C>
    {
        /// <summary>
        /// Gets a value indicating the type of object implementing <see cref=""ISyntax"" />.
        /// </summary>
        public SarifKind SyntaxKind
        {
            get
            {
                return SarifKind.C;
            }
        }

        /// <summary>
        /// An integer property.
        /// </summary>
        public int IntProp { get; set; }

        /// <summary>
        /// A string property.
        /// </summary>
        public string StringProp { get; set; }

        /// <summary>
        /// An array property.
        /// </summary>
        public IList<double> ArrayProp { get; set; }

        /// <summary>
        /// A Uri property.
        /// </summary>
        public Uri UriProp { get; set; }

        /// <summary>
        /// A DateTime property.
        /// </summary>
        public DateTime DateTimeProp { get; set; }
        public D ReferencedTypeProp { get; set; }

        /// <summary>
        /// An array of a cloneable type.
        /// </summary>
        public IList<D> ArrayOfRefProp { get; set; }

        /// <summary>
        /// An array of arrays.
        /// </summary>
        public IList<IList<D>> ArrayOfArrayProp { get; set; }

        /// <summary>
        /// A dictionary property.
        /// </summary>
        public Dictionary<string, string> DictionaryProp { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref=""C"" /> class.
        /// </summary>
        public C()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref=""C"" /> class from the supplied values.
        /// </summary>
        /// <param name=""intProp"">
        /// An initialization value for the <see cref=""P: IntProp"" /> property.
        /// </param>
        /// <param name=""stringProp"">
        /// An initialization value for the <see cref=""P: StringProp"" /> property.
        /// </param>
        /// <param name=""arrayProp"">
        /// An initialization value for the <see cref=""P: ArrayProp"" /> property.
        /// </param>
        /// <param name=""uriProp"">
        /// An initialization value for the <see cref=""P: UriProp"" /> property.
        /// </param>
        /// <param name=""dateTimeProp"">
        /// An initialization value for the <see cref=""P: DateTimeProp"" /> property.
        /// </param>
        /// <param name=""referencedTypeProp"">
        /// An initialization value for the <see cref=""P: ReferencedTypeProp"" /> property.
        /// </param>
        /// <param name=""arrayOfRefProp"">
        /// An initialization value for the <see cref=""P: ArrayOfRefProp"" /> property.
        /// </param>
        /// <param name=""arrayOfArrayProp"">
        /// An initialization value for the <see cref=""P: ArrayOfArrayProp"" /> property.
        /// </param>
        /// <param name=""dictionaryProp"">
        /// An initialization value for the <see cref=""P: DictionaryProp"" /> property.
        /// </param>
        public C(int intProp, string stringProp, IEnumerable<double> arrayProp, Uri uriProp, DateTime dateTimeProp, D referencedTypeProp, IEnumerable<D> arrayOfRefProp, IEnumerable<IEnumerable<D>> arrayOfArrayProp, Dictionary<string, string> dictionaryProp)
        {
            Init(intProp, stringProp, arrayProp, uriProp, dateTimeProp, referencedTypeProp, arrayOfRefProp, arrayOfArrayProp, dictionaryProp);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref=""C"" /> class from the specified instance.
        /// </summary>
        /// <param name=""other"">
        /// The instance from which the new instance is to be initialized.
        /// </param>
        /// <exception cref=""ArgumentNullException"">
        /// Thrown if <paramref name=""other"" /> is null.
        /// </exception>
        public C(C other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            Init(other.IntProp, other.StringProp, other.ArrayProp, other.UriProp, other.DateTimeProp, other.ReferencedTypeProp, other.ArrayOfRefProp, other.ArrayOfArrayProp, other.DictionaryProp);
        }

        ISyntax ISyntax.DeepClone()
        {
            return DeepCloneCore();
        }

        /// <summary>
        /// Creates a deep copy of this instance.
        /// </summary>
        public C DeepClone()
        {
            return (C)DeepCloneCore();
        }

        private ISyntax DeepCloneCore()
        {
            return new C(this);
        }

        private void Init(int intProp, string stringProp, IEnumerable<double> arrayProp, Uri uriProp, DateTime dateTimeProp, D referencedTypeProp, IEnumerable<D> arrayOfRefProp, IEnumerable<IEnumerable<D>> arrayOfArrayProp, Dictionary<string, string> dictionaryProp)
        {
            IntProp = intProp;
            StringProp = stringProp;
            if (arrayProp != null)
            {
                var destination_0 = new List<double>();
                foreach (var value_0 in arrayProp)
                {
                    destination_0.Add(value_0);
                }

                ArrayProp = destination_0;
            }

            if (uriProp != null)
            {
                UriProp = new Uri(uriProp.OriginalString, uriProp.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative);
            }

            DateTimeProp = dateTimeProp;
            if (referencedTypeProp != null)
            {
                ReferencedTypeProp = new D(referencedTypeProp);
            }

            if (arrayOfRefProp != null)
            {
                var destination_1 = new List<D>();
                foreach (var value_1 in arrayOfRefProp)
                {
                    if (value_1 == null)
                    {
                        destination_1.Add(null);
                    }
                    else
                    {
                        destination_1.Add(new D(value_1));
                    }
                }

                ArrayOfRefProp = destination_1;
            }

            if (arrayOfArrayProp != null)
            {
                var destination_2 = new List<IList<D>>();
                foreach (var value_2 in arrayOfArrayProp)
                {
                    if (value_2 == null)
                    {
                        destination_2.Add(null);
                    }
                    else
                    {
                        var destination_3 = new List<D>();
                        foreach (var value_3 in value_2)
                        {
                            if (value_3 == null)
                            {
                                destination_3.Add(null);
                            }
                            else
                            {
                                destination_3.Add(new D(value_3));
                            }
                        }

                        destination_2.Add(destination_3);
                    }
                }

                ArrayOfArrayProp = destination_2;
            }

            if (dictionaryProp != null)
            {
                DictionaryProp = new Dictionary<string, string>(dictionaryProp);
            }
        }
    }
}";
            const string ExpectedSyntaxInterface =
@"namespace N
{
    /// <summary>
    /// An interface for all types generated from the Sarif schema.
    /// </summary>
    public interface ISyntax
    {
        /// <summary>
        /// Gets a value indicating the type of object implementing <see cref=""ISyntax"" />.
        /// </summary>
        SarifKind SyntaxKind { get; }

        /// <summary>
        /// Makes a deep copy of this instance.
        /// </summary>
        ISyntax DeepClone();
    }
}";
            const string ExpectedKindEnum =
@"namespace N
{
    /// <summary>
    /// A set of values for all the types that implement <see cref=""ISyntax"" />.
    /// </summary>
    public enum SarifKind
    {
        /// <summary>
        /// An uninitialized kind.
        /// </summary>
        None,
        /// <summary>
        /// A value indicating that the <see cref=""ISyntax"" /> object is of type <see cref=""C"" />.
        /// </summary>
        C,
        /// <summary>
        /// A value indicating that the <see cref=""ISyntax"" /> object is of type <see cref=""D"" />.
        /// </summary>
        D
    }
}";
            _settings.GenerateCloningCode = true;
            _settings.HintDictionary = HintDictionary.Deserialize(HintsText);
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            generator.Generate(schema);

            string syntaxInterfacePath = TestFileSystem.MakeOutputFilePath("ISyntax");
            string kindEnumPath = TestFileSystem.MakeOutputFilePath("SarifKind");
            string referencedTypePath = TestFileSystem.MakeOutputFilePath("D");

            var expectedOutputFiles = new List<string>
            {
                PrimaryOutputFilePath,
                syntaxInterfacePath,
                kindEnumPath,
                referencedTypePath
            };

            _testFileSystem.Files.Count.Should().Be(expectedOutputFiles.Count);
            _testFileSystem.Files.Should().OnlyContain(path => expectedOutputFiles.Contains(path));

            _testFileSystem[PrimaryOutputFilePath].Should().Be(ExpectedClass);
            _testFileSystem[syntaxInterfacePath].Should().Be(ExpectedSyntaxInterface);
            _testFileSystem[kindEnumPath].Should().Be(ExpectedKindEnum);
        }

        [Fact(DisplayName = "DataModelGenerator generates classes for schemas in definitions")]
        public void GeneratesClassesForSchemasInDefinitions()
        {
            _settings.GenerateOverrides = true;
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = TestUtil.CreateSchemaFromTestDataFile("Definitions");

            generator.Generate(schema);

            const string ExpectedRootClass =
@"using System;

namespace N
{
    public partial class C : IEquatable<C>
    {
        public bool RootProp { get; set; }

        public override bool Equals(object other)
        {
            return Equals(other as C);
        }

        public override int GetHashCode()
        {
            int result = 17;
            unchecked
            {
                result = (result * 31) + RootProp.GetHashCode();
            }

            return result;
        }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }

            if (RootProp != other.RootProp)
            {
                return false;
            }

            return true;
        }
    }
}";

            const string ExpectedDefinedClass1 =
@"using System;

namespace N
{
    public partial class Def1 : IEquatable<Def1>
    {
        public string Prop1 { get; set; }

        public override bool Equals(object other)
        {
            return Equals(other as Def1);
        }

        public override int GetHashCode()
        {
            int result = 17;
            unchecked
            {
                if (Prop1 != null)
                {
                    result = (result * 31) + Prop1.GetHashCode();
                }
            }

            return result;
        }

        public bool Equals(Def1 other)
        {
            if (other == null)
            {
                return false;
            }

            if (Prop1 != other.Prop1)
            {
                return false;
            }

            return true;
        }
    }
}";

            const string ExpectedDefinedClass2 =
@"using System;

namespace N
{
    public partial class Def2 : IEquatable<Def2>
    {
        public int Prop2 { get; set; }

        public override bool Equals(object other)
        {
            return Equals(other as Def2);
        }

        public override int GetHashCode()
        {
            int result = 17;
            unchecked
            {
                result = (result * 31) + Prop2.GetHashCode();
            }

            return result;
        }

        public bool Equals(Def2 other)
        {
            if (other == null)
            {
                return false;
            }

            if (Prop2 != other.Prop2)
            {
                return false;
            }

            return true;
        }
    }
}";
            string def1Path = TestFileSystem.MakeOutputFilePath("Def1");
            string def2Path = TestFileSystem.MakeOutputFilePath("Def2");

            var expectedOutputFiles = new List<string>
            {
                PrimaryOutputFilePath,
                def1Path,
                def2Path
            };

            _testFileSystem.Files.Count.Should().Be(expectedOutputFiles.Count);
            _testFileSystem.Files.Should().OnlyContain(path => expectedOutputFiles.Contains(path));

            _testFileSystem[PrimaryOutputFilePath].Should().Be(ExpectedRootClass);
            _testFileSystem[def1Path].Should().Be(ExpectedDefinedClass1);
            _testFileSystem[def2Path].Should().Be(ExpectedDefinedClass2);
        }

        [Fact(DisplayName = "DataModelGenerator generates date-time-valued properties")]
        public void GeneratesDateTimeValuedProperties()
        {
            _settings.GenerateOverrides = true;
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""properties"": {
  ""startTime"": {
    ""type"": ""string"",
    ""format"": ""date-time""
    }
  }
}");

            const string Expected =
@"using System;

namespace N
{
    public partial class C : IEquatable<C>
    {
        public DateTime StartTime { get; set; }

        public override bool Equals(object other)
        {
            return Equals(other as C);
        }

        public override int GetHashCode()
        {
            int result = 17;
            unchecked
            {
                result = (result * 31) + StartTime.GetHashCode();
            }

            return result;
        }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }

            if (StartTime != other.StartTime)
            {
                return false;
            }

            return true;
        }
    }
}";
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates uri-valued properties")]
        public void GeneratesUriValuedProperties()
        {
            _settings.GenerateOverrides = true;
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""properties"": {
  ""targetFile"": {
    ""type"": ""string"",
    ""format"": ""uri""
    }
  }
}");

            const string Expected =
@"using System;

namespace N
{
    public partial class C : IEquatable<C>
    {
        public Uri TargetFile { get; set; }

        public override bool Equals(object other)
        {
            return Equals(other as C);
        }

        public override int GetHashCode()
        {
            int result = 17;
            unchecked
            {
                if (TargetFile != null)
                {
                    result = (result * 31) + TargetFile.GetHashCode();
                }
            }

            return result;
        }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }

            if (TargetFile != other.TargetFile)
            {
                return false;
            }

            return true;
        }
    }
}";
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates integer property from dictionary reference")]
        public void GeneratesIntegerPropertyFromDictionaryReference()
        {
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""properties"": {
    ""intDefProp"": {
      ""$ref"": ""#/definitions/d""
    },
  },
  ""definitions"": {
    ""d"": {
      ""type"": ""integer""
    }
  }
}");

            const string Expected =
@"using System;

namespace N
{
    public partial class C : IEquatable<C>
    {
        public int IntDefProp { get; set; }
    }
}";
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates array of primitive types by $ref")]
        public void GeneratesArrayOfPrimitiveTypeByReference()
        {
            _settings.GenerateOverrides = true;
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""properties"": {
    ""arrayOfIntByRef"": {
      ""type"": ""array"",
      ""items"": {
        ""$ref"": ""#/definitions/d""
      }
    }
  },
  ""definitions"": {
    ""d"": {
      ""type"": ""integer"",
      }
    }
  }
}");

            const string Expected =
@"using System;
using System.Collections.Generic;

namespace N
{
    public partial class C : IEquatable<C>
    {
        public IList<int> ArrayOfIntByRef { get; set; }

        public override bool Equals(object other)
        {
            return Equals(other as C);
        }

        public override int GetHashCode()
        {
            int result = 17;
            unchecked
            {
                if (ArrayOfIntByRef != null)
                {
                    foreach (var value_0 in ArrayOfIntByRef)
                    {
                        result = result * 31;
                        result = (result * 31) + value_0.GetHashCode();
                    }
                }
            }

            return result;
        }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }

            if (!Object.ReferenceEquals(ArrayOfIntByRef, other.ArrayOfIntByRef))
            {
                if (ArrayOfIntByRef == null || other.ArrayOfIntByRef == null)
                {
                    return false;
                }

                if (ArrayOfIntByRef.Count != other.ArrayOfIntByRef.Count)
                {
                    return false;
                }

                for (int value_0 = 0; value_0 < ArrayOfIntByRef.Count; ++value_0)
                {
                    if (ArrayOfIntByRef[value_0] != other.ArrayOfIntByRef[value_0])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}";
            string actual = generator.Generate(schema);

            TestUtil.WriteTestResultFiles(Expected, actual, nameof(GeneratesArrayOfPrimitiveTypeByReference));

            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates array of arrays of primitive type")]
        public void GeneratesArrayOfArraysOfPrimitiveType()
        {
            _settings.GenerateOverrides = true;
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""properties"": {
    ""arrayOfArrayOfInt"": {
      ""type"": ""array"",
      ""items"": {
        ""$ref"": ""#/definitions/itemType""
      }
    }
  },
  ""definitions"": {
    ""itemType"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""integer""
      }
    }
  }
}");

            const string Expected =
@"using System;
using System.Collections.Generic;

namespace N
{
    public partial class C : IEquatable<C>
    {
        public IList<IList<int>> ArrayOfArrayOfInt { get; set; }

        public override bool Equals(object other)
        {
            return Equals(other as C);
        }

        public override int GetHashCode()
        {
            int result = 17;
            unchecked
            {
                if (ArrayOfArrayOfInt != null)
                {
                    foreach (var value_0 in ArrayOfArrayOfInt)
                    {
                        result = result * 31;
                        if (value_0 != null)
                        {
                            foreach (var value_1 in value_0)
                            {
                                result = result * 31;
                                result = (result * 31) + value_1.GetHashCode();
                            }
                        }
                    }
                }
            }

            return result;
        }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }

            if (!Object.ReferenceEquals(ArrayOfArrayOfInt, other.ArrayOfArrayOfInt))
            {
                if (ArrayOfArrayOfInt == null || other.ArrayOfArrayOfInt == null)
                {
                    return false;
                }

                if (ArrayOfArrayOfInt.Count != other.ArrayOfArrayOfInt.Count)
                {
                    return false;
                }

                for (int value_0 = 0; value_0 < ArrayOfArrayOfInt.Count; ++value_0)
                {
                    if (!Object.ReferenceEquals(ArrayOfArrayOfInt[value_0], other.ArrayOfArrayOfInt[value_0]))
                    {
                        if (ArrayOfArrayOfInt[value_0] == null || other.ArrayOfArrayOfInt[value_0] == null)
                        {
                            return false;
                        }

                        if (ArrayOfArrayOfInt[value_0].Count != other.ArrayOfArrayOfInt[value_0].Count)
                        {
                            return false;
                        }

                        for (int value_1 = 0; value_1 < ArrayOfArrayOfInt[value_0].Count; ++value_1)
                        {
                            if (ArrayOfArrayOfInt[value_0][value_1] != other.ArrayOfArrayOfInt[value_0][value_1])
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}";
            string actual = generator.Generate(schema);

            TestUtil.WriteTestResultFiles(Expected, actual, nameof(GeneratesArrayOfArraysOfPrimitiveType));

            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates array of arrays of object type")]
        public void GeneratesArrayOfArraysOfObjectType()
        {
            _settings.GenerateOverrides = true;
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""properties"": {
    ""arrayOfArrayOfObject"": {
      ""type"": ""array"",
      ""items"": {
        ""$ref"": ""#/definitions/itemType""
      }
    }
  },
  ""definitions"": {
    ""itemType"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object""
      }
    }
  }
}");

            const string Expected =
@"using System;
using System.Collections.Generic;

namespace N
{
    public partial class C : IEquatable<C>
    {
        public IList<IList<object>> ArrayOfArrayOfObject { get; set; }

        public override bool Equals(object other)
        {
            return Equals(other as C);
        }

        public override int GetHashCode()
        {
            int result = 17;
            unchecked
            {
                if (ArrayOfArrayOfObject != null)
                {
                    foreach (var value_0 in ArrayOfArrayOfObject)
                    {
                        result = result * 31;
                        if (value_0 != null)
                        {
                            foreach (var value_1 in value_0)
                            {
                                result = result * 31;
                                if (value_1 != null)
                                {
                                    result = (result * 31) + value_1.GetHashCode();
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }

            if (!Object.ReferenceEquals(ArrayOfArrayOfObject, other.ArrayOfArrayOfObject))
            {
                if (ArrayOfArrayOfObject == null || other.ArrayOfArrayOfObject == null)
                {
                    return false;
                }

                if (ArrayOfArrayOfObject.Count != other.ArrayOfArrayOfObject.Count)
                {
                    return false;
                }

                for (int value_0 = 0; value_0 < ArrayOfArrayOfObject.Count; ++value_0)
                {
                    if (!Object.ReferenceEquals(ArrayOfArrayOfObject[value_0], other.ArrayOfArrayOfObject[value_0]))
                    {
                        if (ArrayOfArrayOfObject[value_0] == null || other.ArrayOfArrayOfObject[value_0] == null)
                        {
                            return false;
                        }

                        if (ArrayOfArrayOfObject[value_0].Count != other.ArrayOfArrayOfObject[value_0].Count)
                        {
                            return false;
                        }

                        for (int value_1 = 0; value_1 < ArrayOfArrayOfObject[value_0].Count; ++value_1)
                        {
                            if (!Object.Equals(ArrayOfArrayOfObject[value_0][value_1], other.ArrayOfArrayOfObject[value_0][value_1]))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}";
            string actual = generator.Generate(schema);

            TestUtil.WriteTestResultFiles(Expected, actual, nameof(GeneratesArrayOfArraysOfObjectType));

            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates array of arrays of class type")]
        public void GeneratesArrayOfArraysOfClassType()
        {
            _settings.GenerateOverrides = true;
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = SchemaReader.ReadSchema(
@"{
  ""type"": ""object"",
  ""properties"": {
    ""arrayOfArrayOfD"": {
      ""type"": ""array"",
      ""items"": {
        ""$ref"": ""#/definitions/itemType""
      }
    }
  },
  ""definitions"": {
    ""itemType"": {
      ""type"": ""array"",
      ""items"": {
        ""$ref"": ""#/definitions/d""
      }
    },
    ""d"": {
      ""type"": ""object"",
      }
    }
  }
}");

            const string Expected =
@"using System;
using System.Collections.Generic;

namespace N
{
    public partial class C : IEquatable<C>
    {
        public IList<IList<D>> ArrayOfArrayOfD { get; set; }

        public override bool Equals(object other)
        {
            return Equals(other as C);
        }

        public override int GetHashCode()
        {
            int result = 17;
            unchecked
            {
                if (ArrayOfArrayOfD != null)
                {
                    foreach (var value_0 in ArrayOfArrayOfD)
                    {
                        result = result * 31;
                        if (value_0 != null)
                        {
                            foreach (var value_1 in value_0)
                            {
                                result = result * 31;
                                if (value_1 != null)
                                {
                                    result = (result * 31) + value_1.GetHashCode();
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        public bool Equals(C other)
        {
            if (other == null)
            {
                return false;
            }

            if (!Object.ReferenceEquals(ArrayOfArrayOfD, other.ArrayOfArrayOfD))
            {
                if (ArrayOfArrayOfD == null || other.ArrayOfArrayOfD == null)
                {
                    return false;
                }

                if (ArrayOfArrayOfD.Count != other.ArrayOfArrayOfD.Count)
                {
                    return false;
                }

                for (int value_0 = 0; value_0 < ArrayOfArrayOfD.Count; ++value_0)
                {
                    if (!Object.ReferenceEquals(ArrayOfArrayOfD[value_0], other.ArrayOfArrayOfD[value_0]))
                    {
                        if (ArrayOfArrayOfD[value_0] == null || other.ArrayOfArrayOfD[value_0] == null)
                        {
                            return false;
                        }

                        if (ArrayOfArrayOfD[value_0].Count != other.ArrayOfArrayOfD[value_0].Count)
                        {
                            return false;
                        }

                        for (int value_1 = 0; value_1 < ArrayOfArrayOfD[value_0].Count; ++value_1)
                        {
                            if (!Object.Equals(ArrayOfArrayOfD[value_0][value_1], other.ArrayOfArrayOfD[value_0][value_1]))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}";
            string actual = generator.Generate(schema);

            TestUtil.WriteTestResultFiles(Expected, actual, nameof(GeneratesArrayOfArraysOfClassType));

            actual.Should().Be(Expected);
        }

        [Fact(DisplayName = "DataModelGenerator generates string for inline enum of string")]
        public void GeneratesStringForInlineEnumOfString()
        {
            var generator = new DataModelGenerator(_settings, _testFileSystem.FileSystem);

            JsonSchema schema = SchemaReader.ReadSchema(
    @"{
  ""type"": ""object"",
  ""properties"": {
    ""version"": {
      ""enum"": [
        ""v1.0"",
        ""v2.0""
      ]
    }
  }
}");

            const string Expected =
@"using System;

namespace N
{
    public partial class C : IEquatable<C>
    {
        public string Version { get; set; }
    }
}";
            string actual = generator.Generate(schema);
            actual.Should().Be(Expected);
        }
    }
}
