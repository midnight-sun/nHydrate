#pragma warning disable 0168
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data;
using nHydrate.Generator.Models;
using System.Collections;

namespace nHydrate.Generator.SQLInstaller.ProjectItemGenerators
{
    internal static class Globals
    {
        public static string GetDateTimeNowCode(ModelRoot model)
        {
            return model.UseUTCTime ? "DateTime.UtcNow" : "DateTime.Now";
        }

        public static IEnumerable<Column> GetValidSearchColumns(Table _currentTable)
        {
            try
            {
                var validColumns = new List<Column>();
                foreach (var column in _currentTable.GeneratedColumns)
                {
                    if (!(column.DataType == System.Data.SqlDbType.Binary ||
                        column.DataType == System.Data.SqlDbType.Image ||
                        column.DataType == System.Data.SqlDbType.NText ||
                        column.DataType == System.Data.SqlDbType.Text ||
                        column.DataType == System.Data.SqlDbType.Timestamp ||
                        column.DataType == System.Data.SqlDbType.Udt ||
                        column.DataType == System.Data.SqlDbType.VarBinary ||
                        column.DataType == System.Data.SqlDbType.Variant ||
                    column.DataType == System.Data.SqlDbType.Money))
                    {
                        validColumns.Add(column);
                    }
                }
                return validColumns.OrderBy(x => x.Name).AsEnumerable();

            }
            catch (Exception ex)
            {
                throw new Exception(_currentTable.DatabaseName + ": Failed on generation of select or template", ex);
            }
        }

        public static void AppendBusinessEntryCatch(StringBuilder sb)
        {
            sb.AppendLine("			catch (System.Data.DBConcurrencyException dbcex)");
            sb.AppendLine("			{");
            sb.AppendLine("				throw new ConcurrencyException(\"Concurrency failure\", dbcex);");
            sb.AppendLine("			}");
            sb.AppendLine("			catch (System.Data.SqlClient.SqlException sqlexp)");
            sb.AppendLine("			{");
            sb.AppendLine("				if (sqlexp.Number == 547 || sqlexp.Number == 2627)");
            sb.AppendLine("				{");
            sb.AppendLine("					throw new UniqueConstraintViolatedException(\"Constraint Failure\", sqlexp);");
            sb.AppendLine("				}");
            sb.AppendLine("				else");
            sb.AppendLine("				{");
            sb.AppendLine("					throw;");
            sb.AppendLine("				}");
            sb.AppendLine("			}");
            sb.AppendLine("			catch(Exception ex)");
            sb.AppendLine("			{");
            sb.AppendLine("				System.Diagnostics.Debug.WriteLine(ex.ToString());");
            sb.AppendLine("				throw;");
            sb.AppendLine("			}");
        }

        public static string BuildSelectList(Table table, ModelRoot model)
        {
            return BuildSelectList(table, model, false);
        }

        public static string BuildSelectList(Table table, ModelRoot model, bool useFullHierarchy)
        {
            var index = 0;
            var output = new StringBuilder();
            var columnList = new List<Column>();
            if (useFullHierarchy)
            {
                foreach (var c in table.GetColumnsFullHierarchy().Where(x => x.Generated).OrderBy(x => x.Name))
                    columnList.Add(c);
            }
            else
            {
                columnList.AddRange(table.GeneratedColumns);
            }

            foreach (var column in columnList.OrderBy(x => x.Name))
            {
                var parentTable = column.ParentTable;
                output.AppendFormat("\t[{2}].[{0}].[{1}]", GetTableDatabaseName(model, parentTable), column.DatabaseName, parentTable.GetSQLSchema());
                if ((index < columnList.Count - 1) || (table.AllowCreateAudit) || (table.AllowModifiedAudit) || (table.AllowTimestamp))
                    output.Append(",");
                output.AppendLine();
                index++;
            }

            if (table.AllowCreateAudit)
            {
                output.AppendFormat("	[{2}].[{0}].[{1}],", GetTableDatabaseName(model, table), model.Database.CreatedByColumnName, table.GetSQLSchema());
                output.AppendLine();

                output.AppendFormat("	[{2}].[{0}].[{1}]", GetTableDatabaseName(model, table), model.Database.CreatedDateColumnName, table.GetSQLSchema());
                if ((table.AllowModifiedAudit) || (table.AllowTimestamp))
                    output.Append(",");
                output.AppendLine();
            }

            if (table.AllowModifiedAudit)
            {
                output.AppendFormat("	[{2}].[{0}].[{1}],", GetTableDatabaseName(model, table), model.Database.ModifiedByColumnName, table.GetSQLSchema());
                output.AppendLine();

                output.AppendFormat("	[{2}].[{0}].[{1}]", GetTableDatabaseName(model, table), model.Database.ModifiedDateColumnName, table.GetSQLSchema());
                if (table.AllowTimestamp)
                    output.Append(",");
                output.AppendLine();
            }

            if (table.AllowTimestamp)
            {
                output.AppendFormat("	[{2}].[{0}].[{1}]", GetTableDatabaseName(model, table.GetAbsoluteBaseTable()), model.Database.TimestampColumnName, table.GetAbsoluteBaseTable().GetSQLSchema());
                output.AppendLine();
            }

            return output.ToString();
        }

        public static string BuildPrimaryKeySelectList(ModelRoot model, Table table, bool qualifiedNames)
        {
            var index = 0;
            var output = new StringBuilder();
            foreach (var column in table.PrimaryKeyColumns.OrderBy(x => x.Name))
            {
                output.Append("	[");
                if (qualifiedNames)
                {
                    output.Append(Globals.GetTableDatabaseName(model, table));
                    output.Append("].[");
                }
                output.Append(column.DatabaseName + "]");
                if (index < table.PrimaryKeyColumns.Count - 1)
                    output.Append(",");
                output.AppendLine();
                index++;
            }
            return output.ToString();
        }

        public static string GetTableDatabaseName(ModelRoot model, Table table)
        {
            return table.DatabaseName;
        }

        public static Column GetColumnByName(ReferenceCollection referenceCollection, string name)
        {
            foreach (Reference r in referenceCollection)
            {
                if (r.Object is Column)
                {
                    if (string.Compare(((Column)r.Object).Name, name, true) == 0)
                        return (Column)r.Object;
                }
            }
            return null;
        }

        public static Column GetColumnByKey(ReferenceCollection referenceCollection, string columnKey)
        {
            foreach (Reference r in referenceCollection)
            {
                if (r.Object is Column)
                {
                    if (string.Compare(((Column)r.Object).Key, columnKey, true) == 0)
                        return (Column)r.Object;
                }
            }
            return null;
        }

        public static void AppendCreateAudit(Table table, ModelRoot model, StringBuilder sb)
        {
            try
            {
                var dateTimeString = (model.SQLServerType == Common.GeneratorFramework.SQLServerTypeConstants.SQL2005) ? "[DateTime]" : "[DateTime2]";
                sb.AppendLine("--APPEND AUDIT TRAIL CREATE FOR TABLE [" + table.DatabaseName + "]");
                sb.AppendLine($"if exists(select * from sys.tables where name = '{table.DatabaseName}') and not exists (select * from sys.columns c inner join sys.tables t on c.object_id = t.object_id where c.name = '{model.Database.CreatedByColumnName}' and t.name = '{table.DatabaseName}')");
                sb.AppendLine($"ALTER TABLE [{table.GetSQLSchema()}].[{table.DatabaseName}] ADD [{model.Database.CreatedByColumnName}] [NVarchar] (50) NULL");
                var dfName = "DF__" + table.DatabaseName + "_" + model.Database.CreatedDateColumnName;
                dfName = dfName.ToUpper();
                sb.AppendLine($"if exists(select * from sys.tables where name = '{table.DatabaseName}') and not exists (select * from sys.columns c inner join sys.tables t on c.object_id = t.object_id where c.name = '{model.Database.CreatedDateColumnName}' and t.name = '{table.DatabaseName}')");
                sb.AppendLine($"ALTER TABLE [{table.GetSQLSchema()}].[{table.DatabaseName}] ADD [{model.Database.CreatedDateColumnName}] " + dateTimeString + " CONSTRAINT [" + dfName + "] DEFAULT " + model.GetSQLDefaultDate() + " NULL");
                sb.AppendLine("GO");
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static void AppendModifiedAudit(Table table, ModelRoot model, StringBuilder sb)
        {
            try
            {
                var dateTimeString = (model.SQLServerType == Common.GeneratorFramework.SQLServerTypeConstants.SQL2005) ? "[DateTime]" : "[DateTime2]";
                sb.AppendLine("--APPEND AUDIT TRAIL MODIFY FOR TABLE [" + table.DatabaseName + "]");
                sb.AppendLine($"if exists(select * from sys.tables where name = '{table.DatabaseName}') and not exists (select * from sys.columns c inner join sys.tables t on c.object_id = t.object_id where c.name = '{model.Database.ModifiedByColumnName}' and t.name = '{table.DatabaseName}')");
                sb.AppendLine($"ALTER TABLE [{table.GetSQLSchema()}].[{table.DatabaseName}] ADD [{model.Database.ModifiedByColumnName}] [NVarchar] (50) NULL");
                var dfName = "DF__" + table.DatabaseName + "_" + model.Database.ModifiedDateColumnName;
                dfName = dfName.ToUpper();
                sb.AppendLine($"if exists(select * from sys.tables where name = '{table.DatabaseName}') and not exists (select * from sys.columns c inner join sys.tables t on c.object_id = t.object_id where c.name = '{model.Database.ModifiedDateColumnName}' and t.name = '{table.DatabaseName}')");
                sb.AppendLine($"ALTER TABLE [{table.GetSQLSchema()}].[{table.DatabaseName}] ADD [{model.Database.ModifiedDateColumnName}] " + dateTimeString + " CONSTRAINT [" + dfName + "] DEFAULT " + model.GetSQLDefaultDate() + " NULL");
                sb.AppendLine("GO");
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static void AppendTimestampAudit(Table table, ModelRoot model, StringBuilder sb)
        {
            try
            {
                sb.AppendLine("--APPEND AUDIT TRAIL TIMESTAMP FOR TABLE [" + table.DatabaseName + "]");
                sb.AppendLine($"if exists(select * from sys.tables where name = '{table.DatabaseName}') and not exists (select * from sys.columns c inner join sys.objects o on c.object_id = o.object_id where c.name = '" + model.Database.TimestampColumnName + "' and o.name = '" + table.DatabaseName + "')");
                sb.AppendLine($"ALTER TABLE [{table.GetSQLSchema()}].[{table.DatabaseName}] ADD [" + model.Database.TimestampColumnName + "] [ROWVERSION] NOT NULL");
                sb.AppendLine("GO");
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static void DropCreateAudit(Table table, ModelRoot model, StringBuilder sb)
        {
            sb.AppendLine("--REMOVE AUDIT TRAIL CREATE FOR TABLE [" + table.DatabaseName + "]");
            sb.AppendLine($"if exists (select * from sys.columns c inner join sys.objects o on c.object_id = o.object_id where c.name = '{model.Database.CreatedByColumnName}' and o.name = '{table.DatabaseName}')");
            sb.AppendLine($"ALTER TABLE [{table.GetSQLSchema()}].[{table.DatabaseName}] DROP COLUMN [{model.Database.CreatedByColumnName}]");
            var dfName = $"DF__{table.DatabaseName}_{model.Database.CreatedDateColumnName}".ToUpper();
            sb.AppendLine("if exists (select * from sys.objects where name = '" + dfName + "' and [type] = 'D')");
            sb.AppendLine($"ALTER TABLE [{table.GetSQLSchema()}].[{table.DatabaseName}] DROP CONSTRAINT [" + dfName + "]");
            sb.AppendLine($"if exists (select * from sys.columns c inner join sys.objects o on c.object_id = o.object_id where c.name = '{model.Database.CreatedDateColumnName}' and o.name = '{table.DatabaseName}')");
            sb.AppendLine($"ALTER TABLE [{table.GetSQLSchema()}].[{table.DatabaseName}] DROP COLUMN [{model.Database.CreatedDateColumnName}]");
            sb.AppendLine("GO");
            sb.AppendLine();
        }

        public static void DropModifiedAudit(Table table, ModelRoot model, StringBuilder sb)
        {
            sb.AppendLine($"--REMOVE AUDIT TRAIL MODIFY FOR TABLE [{table.DatabaseName}]");
            sb.AppendLine($"if exists (select * from sys.columns c inner join sys.objects o on c.object_id = o.object_id where c.name = '{model.Database.ModifiedByColumnName}' and o.name = '{table.DatabaseName}')");
            sb.AppendLine($"ALTER TABLE [{table.GetSQLSchema()}].[{table.DatabaseName}] DROP COLUMN [{model.Database.ModifiedByColumnName}]");
            var dfName = $"DF__{table.DatabaseName}_{model.Database.ModifiedDateColumnName}".ToUpper();
            sb.AppendLine($"if exists (select * from sys.objects where name = '{dfName}' and [type] = 'D')");
            sb.AppendLine($"ALTER TABLE [{table.GetSQLSchema()}].[{table.DatabaseName}] DROP CONSTRAINT [" + dfName + "]");
            sb.AppendLine($"if exists (select * from sys.columns c inner join sys.objects o on c.object_id = o.object_id where c.name = '{model.Database.ModifiedDateColumnName}' and o.name = '{table.DatabaseName}')");
            sb.AppendLine($"ALTER TABLE [{table.GetSQLSchema()}].[{table.DatabaseName}] DROP COLUMN [{model.Database.ModifiedDateColumnName}]");
            sb.AppendLine("GO");
            sb.AppendLine();
        }

        public static void DropTimestampAudit(Table table, ModelRoot model, StringBuilder sb)
        {
            sb.AppendLine($"--REMOVE AUDIT TRAIL TIMESTAMP FOR TABLE [{table.DatabaseName}]");
            sb.AppendLine($"if exists (select * from sys.columns c inner join sys.objects o on c.object_id = o.object_id where c.name = '{model.Database.TimestampColumnName}' and o.name = '{table.DatabaseName}')");
            sb.AppendLine($"ALTER TABLE [{table.GetSQLSchema()}].[{table.DatabaseName}] DROP COLUMN [{model.Database.TimestampColumnName}]");
            sb.AppendLine("GO");
            sb.AppendLine();
        }

    }
}