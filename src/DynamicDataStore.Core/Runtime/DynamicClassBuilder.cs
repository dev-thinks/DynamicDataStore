using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using DynamicDataStore.Core.Db;
using DynamicDataStore.Core.Model;
using DynamicDataStore.Core.Util;
using Microsoft.EntityFrameworkCore;

namespace DynamicDataStore.Core.Runtime
{
    public class DynamicClassBuilder
    {
        public Dictionary<string, Type> Types { get; set; }

        public Dictionary<string, TypeBuilder> TypeBuilders { get; set; }

        public TypeBuilder ContextTypeBuilder { get; set; }

        private readonly Config _config;

        private readonly DynamicTypeBuilder _typeBuilder;

        public DynamicClassBuilder(Config config)
        {
            Types = new Dictionary<string, Type>();
            TypeBuilders = new Dictionary<string, TypeBuilder>();
            _config = config;
            _typeBuilder = new DynamicTypeBuilder();
        }

        public Type CreateContextType(List<Table> tables, string cString)
        {
            Types.Clear();
            TypeBuilders.Clear();

            //Context
            ContextTypeBuilder = _typeBuilder.GetTypeBuilder("DynamicDbContext", typeof(DbContextBase));

            //Context Constructor
            System.Reflection.Emit.ConstructorBuilder constructor = ContextTypeBuilder.DefineDefaultConstructor(
                System.Reflection.MethodAttributes.Public |
                System.Reflection.MethodAttributes.SpecialName |
                System.Reflection.MethodAttributes.RTSpecialName);

            //Create Normal Poco Type to be used as a reference
            foreach (Table table in tables)
            {
                TypeBuilders.Add(table.VariableName, CreatePocoTypeBuilder(table));
            }

            //Navigation properties
            foreach (Table table in tables)
            {
                CreateNavigationProperties(table);
            }

            //Creates DbSet Properties for the Context
            foreach (Table ti in tables)
            {
                var pocoTypeBuilder = TypeBuilders[ti.VariableName];
                var pocoType = pocoTypeBuilder.CreateType();
                Types.Add(ti.VariableName, pocoType);

                _typeBuilder.CreateProperty(ContextTypeBuilder, ti.VariableName,
                    typeof(DbSet<>).MakeGenericType(new Type[] {pocoType}), false);
            }

            // context OnConfiguring method override.
            var onConfiguringMethod = ContextTypeBuilder.OverrideOnConfiguring(cString);
            ContextTypeBuilder.DefineMethodOverride(onConfiguringMethod,
                typeof(DbContext).GetMethod("OnConfiguring", BindingFlags.Instance | BindingFlags.NonPublic));

            Type type = ContextTypeBuilder.CreateType();

            if (_config.SaveLibraryRunTime)
            {
                _typeBuilder.SaveTypeBuilder(ContextTypeBuilder, "DynamicDataStore");
            }

            return type;
        }

        private void CreateNavigationProperties(Table table)
        {
            PropertyInfo pi;
            TypeBuilder fkTypeBuilder;
            TypeBuilder collectionTypeBuilder;
            TypeBuilder builder;

            foreach (Column column in table.Columns)
            {
                if (column.IsFk)
                {
                    builder = TypeBuilders[table.VariableName];

                    //Creating FK Object
                    fkTypeBuilder = TypeBuilders[column.ReferencedVariableName];
                    PropertyBuilder pb = _typeBuilder.CreateVirtualProperty(builder,
                        column.ColumnName + _config.ObjectPostfixName, fkTypeBuilder);

                    //DisplayName Attribute
                    ConstructorInfo displayNameAttributeBuilder =
                        typeof(DisplayNameAttribute).GetConstructor(new Type[] {typeof(string)});
                    pi = typeof(DisplayNameAttribute).GetProperties().FirstOrDefault(o => o.Name == "DisplayName");
                    var attribute = new CustomAttributeBuilder(displayNameAttributeBuilder,
                        new object[] {Utils.GetFancyLabel(column.ColumnName + _config.ObjectPostfixName)});
                    pb.SetCustomAttribute(attribute);

                    //Browsable Attribute
                    ConstructorInfo browsableAttributeBuilder =
                        typeof(BrowsableAttribute).GetConstructor(new Type[] {typeof(bool)});
                    pi = typeof(BrowsableAttribute).GetProperties().FirstOrDefault(o => o.Name == "Browsable");
                    attribute = new CustomAttributeBuilder(browsableAttributeBuilder, new object[] {false});
                    pb.SetCustomAttribute(attribute);

                    //foreignKey Attribute
                    ConstructorInfo foreignKeyAttributeBuilder =
                        typeof(ForeignKeyAttribute).GetConstructor(new Type[] {typeof(string)});
                    pi = typeof(ForeignKeyAttribute).GetProperties().FirstOrDefault(o => o.Name == "Name");
                    attribute = new CustomAttributeBuilder(foreignKeyAttributeBuilder,
                        new object[] {column.ColumnName});
                    pb.SetCustomAttribute(attribute);

                    //Creating Collection Object for the referenced table
                    builder = TypeBuilders[column.ReferencedVariableName];

                    collectionTypeBuilder = TypeBuilders[table.VariableName];
                    pb = _typeBuilder.CreateVirtualProperty(builder,
                        column.TableName + _config.CollectionPostfixName + "From" + column.ColumnName,
                        typeof(CustomList<>).MakeGenericType(new Type[] {collectionTypeBuilder.UnderlyingSystemType}));

                    //InverseProperty Attribute
                    ConstructorInfo inversePropertyAttributeBuilder =
                        typeof(InversePropertyAttribute).GetConstructor(new Type[] {typeof(string)});
                    pi = typeof(InversePropertyAttribute).GetProperties().FirstOrDefault(o => o.Name == "Property");
                    attribute = new CustomAttributeBuilder(inversePropertyAttributeBuilder,
                        new object[] {column.ColumnName + _config.ObjectPostfixName});
                    pb.SetCustomAttribute(attribute);

                    //DisplayName Attribute
                    displayNameAttributeBuilder =
                        typeof(DisplayNameAttribute).GetConstructor(new Type[] {typeof(string)});
                    pi = typeof(DisplayNameAttribute).GetProperties().FirstOrDefault(o => o.Name == "DisplayName");
                    attribute = new CustomAttributeBuilder(displayNameAttributeBuilder,
                        new object[]
                        {
                            Utils.GetFancyLabel(column.TableName + _config.CollectionPostfixName + "From" +
                                                column.ColumnName)
                        });
                    pb.SetCustomAttribute(attribute);

                    //Browsable Attribute
                    browsableAttributeBuilder = typeof(BrowsableAttribute).GetConstructor(new Type[] {typeof(bool)});
                    pi = typeof(BrowsableAttribute).GetProperties().FirstOrDefault(o => o.Name == "Browsable");
                    attribute = new CustomAttributeBuilder(browsableAttributeBuilder, new object[] {false});
                    pb.SetCustomAttribute(attribute);
                }
            }
        }

        private TypeBuilder CreatePocoTypeBuilder(Table table)
        {
            Type propertyType;
            PropertyBuilder propertyBuilder;
            PropertyInfo pi;

            TypeBuilder builder = _typeBuilder.GetTypeBuilder(table.VariableName, typeof(PocoBase));

            ConstructorBuilder constructor = builder.DefineDefaultConstructor(
                System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.SpecialName |
                System.Reflection.MethodAttributes.RTSpecialName);

            //DataContract Attribute
            ConstructorInfo dataContractAttributeBuilder = typeof(DataContractAttribute).GetConstructor(new Type[] { });
            pi = typeof(DataContractAttribute).GetProperties().FirstOrDefault(o => o.Name == "IsReference");
            var attribute = new CustomAttributeBuilder(dataContractAttributeBuilder, new object[] { },
                new PropertyInfo[] {pi}, new object[] {true});
            builder.SetCustomAttribute(attribute);

            //Table Schema Attribute
            ConstructorInfo tableAttributeBuilder = typeof(TableAttribute).GetConstructor(new Type[] {typeof(string)});
            pi = typeof(TableAttribute).GetProperties().FirstOrDefault(o => o.Name == "Schema");
            attribute = new CustomAttributeBuilder(tableAttributeBuilder, new object[] {table.Name},
                new PropertyInfo[] {pi}, new object[] {table.Schema});
            builder.SetCustomAttribute(attribute);

            //Creating normal properties for each poco class
            foreach (Column column in table.Columns)
            {
                propertyType = column.DataType.SystemType;
                propertyBuilder = _typeBuilder.CreateProperty(builder, column.ColumnName, propertyType, true);

                //DisplayName Attribute
                ConstructorInfo displayNameAttributeBuilder =
                    typeof(DisplayNameAttribute).GetConstructor(new Type[] {typeof(string)});
                pi = typeof(DisplayNameAttribute).GetProperties().FirstOrDefault(o => o.Name == "DisplayName");
                attribute = new CustomAttributeBuilder(displayNameAttributeBuilder,
                    new object[] {Utils.GetFancyLabel(column.ColumnName)});
                propertyBuilder.SetCustomAttribute(attribute);

                if (column.IsPk)
                {
                    //Key Attribute
                    ConstructorInfo keyAttributeBuilder =
                        typeof(System.ComponentModel.DataAnnotations.KeyAttribute).GetConstructor(new Type[] { });
                    attribute = new CustomAttributeBuilder(keyAttributeBuilder, new object[] { });
                    propertyBuilder.SetCustomAttribute(attribute);

                    //Column Attribute
                    ConstructorInfo columnAttributeBuilder =
                        typeof(System.ComponentModel.DataAnnotations.Schema.ColumnAttribute).GetConstructor(new Type[]
                            { });
                    pi = typeof(System.ComponentModel.DataAnnotations.Schema.ColumnAttribute).GetProperties()
                        .FirstOrDefault(o => o.Name == "Order");
                    attribute = new CustomAttributeBuilder(columnAttributeBuilder, new object[] { },
                        new PropertyInfo[] {pi}, new object[] {column.PkPosition});
                    propertyBuilder.SetCustomAttribute(attribute);

                    if (!column.PkIsIdentity)
                    {
                        ConstructorInfo identityAttributeBuilder =
                            typeof(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute)
                                .GetConstructor(new Type[] {typeof(DatabaseGeneratedOption)});
                        pi = typeof(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute)
                            .GetProperties().FirstOrDefault(o => o.Name == "DatabaseGeneratedOption");
                        attribute = new CustomAttributeBuilder(identityAttributeBuilder,
                            new object[] {DatabaseGeneratedOption.None});
                        propertyBuilder.SetCustomAttribute(attribute);
                    }
                    else
                    {
                        ConstructorInfo identityAttributeBuilder =
                            typeof(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute)
                                .GetConstructor(new Type[] {typeof(DatabaseGeneratedOption)});
                        pi = typeof(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute)
                            .GetProperties().FirstOrDefault(o => o.Name == "DatabaseGeneratedOption");
                        attribute = new CustomAttributeBuilder(identityAttributeBuilder,
                            new object[] {DatabaseGeneratedOption.Identity});
                        propertyBuilder.SetCustomAttribute(attribute);
                    }
                }

                //DataMember Attribute
                ConstructorInfo dataMemberAttributeBuilder =
                    typeof(System.Runtime.Serialization.DataMemberAttribute).GetConstructor(new Type[] { });
                attribute = new CustomAttributeBuilder(dataMemberAttributeBuilder, new object[] { });
                propertyBuilder.SetCustomAttribute(attribute);


                bool browsable = column.Browsable;
                browsable = browsable | (column.IsFk && _config.BrowseForeignKeyColumns) |
                            (column.IsPk && _config.BrowsePrimaryKeyColumns);

                //Browsable Attribute
                ConstructorInfo browsableAttributeBuilder =
                    typeof(BrowsableAttribute).GetConstructor(new Type[] {typeof(bool)});
                pi = typeof(BrowsableAttribute).GetProperties().FirstOrDefault(o => o.Name == "Browsable");
                attribute = new CustomAttributeBuilder(browsableAttributeBuilder, new object[] { browsable });
                propertyBuilder.SetCustomAttribute(attribute);
            }

            return builder;
        }
    }
}
