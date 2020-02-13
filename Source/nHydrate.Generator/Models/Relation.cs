#pragma warning disable 0168
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml;
using nHydrate.Generator.Common.GeneratorFramework;
using nHydrate.Generator.Common.Util;

namespace nHydrate.Generator.Models
{
    public class Relation : BaseModelObject
    {
        #region Member Variables

        public enum DeleteActionConstants
        {
            NoAction,
            Cascade,
            SetNull
        }

        protected const string _def_roleName = "";
        protected const string _def_constraintname = "";
        protected const bool _def_enforce = true;
        protected const string _def_description = "";
        protected const DeleteActionConstants _def_deleteAction = DeleteActionConstants.NoAction;

        protected Reference _parentTableRef = null;
        protected Reference _childTableRef = null;
        protected string _roleName = _def_roleName;
        protected string _constraintName = string.Empty;
        protected ColumnRelationshipCollection _columnRelationships = null;
        private bool _enforce = _def_enforce;
        private string _description = _def_description;
        private DeleteActionConstants _deleteAction = _def_deleteAction;

        #endregion

        #region Constructor

        public Relation(INHydrateModelObject root)
            : base(root)
        {
            _columnRelationships = new ColumnRelationshipCollection(this.Root);
        }

        public Relation()
        {
            //Only needed for BaseModelCollection<T>
        }

        #endregion

        #region Events

        public event System.EventHandler BeforeChildTableChange;
        public event System.EventHandler BeforeParentTableChange;
        public event System.EventHandler AfterChildTableChange;
        public event System.EventHandler AfterParentTableChange;

        protected virtual void OnBeforeChildTableChange(object sender, System.EventArgs e)
        {
            if (this.BeforeChildTableChange != null)
                this.BeforeChildTableChange(sender, e);
        }

        protected virtual void OnBeforeParentTableChange(object sender, System.EventArgs e)
        {
            if (this.BeforeParentTableChange != null)
                this.BeforeParentTableChange(sender, e);
        }

        protected virtual void OnAfterChildTableChange(object sender, System.EventArgs e)
        {
            if (this.AfterChildTableChange != null)
                this.AfterChildTableChange(sender, e);
        }

        protected virtual void OnAfterParentTableChange(object sender, System.EventArgs e)
        {
            if (this.AfterParentTableChange != null)
                this.AfterParentTableChange(sender, e);
        }

        #endregion

        #region Property Implementations

        /// <summary>
        /// EF only supports relations where the primary table is from the PK
        /// If the parent table is from a non-PK unique field, EF will NOT render it
        /// </summary>
        public bool IsValidEFRelation
        {
            get { return this.ColumnRelationships.AsEnumerable().All(cr => cr.ParentColumn.PrimaryKey); }
        }

        /// <summary>
        /// Determines the field mappings of this relationship.
        /// </summary>
        [Description("Determines the field mappings of this relationship.")]
        [Category("Data")]
        public ColumnRelationshipCollection ColumnRelationships
        {
            get { return _columnRelationships; }
        }

        /// <summary>
        /// Determines the parent table in the relationship.
        /// </summary>
        [Browsable(false)]
        [Description("Determines the parent table in the relationship.")]
        [Category("Data")]
        public Reference ParentTableRef
        {
            get { return _parentTableRef; }
            set
            {
                if (_parentTableRef != value)
                {
                    this.OnBeforeParentTableChange(this, new EventArgs());
                    _parentTableRef = value;
                    this.RefreshRoleName();
                    this.OnAfterParentTableChange(this, new EventArgs());
                    this.OnPropertyChanged(this, new PropertyChangedEventArgs("parentTableRef"));
                }
            }
        }

        /// <summary>
        /// Determines the child table in the relationship.
        /// </summary>
        [Description("Determines the child table in the relationship.")]
        [Category("Data")]
        public Reference ChildTableRef
        {
            get { return _childTableRef; }
            set
            {
                if (_childTableRef != value)
                {
                    this.OnBeforeChildTableChange(this, new EventArgs());
                    _childTableRef = value;
                    this.RefreshRoleName();
                    this.OnAfterChildTableChange(this, new EventArgs());
                    this.OnPropertyChanged(this, new PropertyChangedEventArgs("childTableRef"));
                }
            }
        }

        /// <summary>
        /// Determines the database role name of this relation.
        /// </summary>
        [Description("Determines the database role name of this relation.")]
        [Category("Data")]
        [DefaultValue(_def_roleName)]
        public string RoleName
        {
            get { return _roleName; }
            set
            {
                if (_roleName != value)
                {
                    _roleName = value;
                    this.OnPropertyChanged(this, new PropertyChangedEventArgs("RoleName"));
                }
            }
        }

        [Browsable(false)]
        public string ConstraintName
        {
            get { return _constraintName; }
            set
            {
                if (_constraintName != value)
                {
                    _constraintName = value;
                    this.OnPropertyChanged(this, new PropertyChangedEventArgs("ConstraintName"));
                }
            }
        }

        /// <summary>
        /// Determines if this relationship has nullable fields or is required
        /// </summary>
        [Browsable(false)]
        public bool IsRequired
        {
            get
            {
                var retval = false;
                foreach (var cr in this.ColumnRelationships.AsEnumerable())
                {
                    retval |= !cr.ChildColumn.AllowNull;
                }
                return retval;
            }
        }

        /// <summary>
        /// Determines if this is a M:N relationship
        /// </summary>
        [Browsable(false)]
        public bool IsManyToMany
        {
            get
            {
                var parentTable = this.ParentTable;
                var childTable = this.ChildTable;
                var otherTable = parentTable;
                if (childTable.AssociativeTable) otherTable = childTable;

                if (otherTable.AssociativeTable)
                {
                    //The associative table must have exactly 2 relations
                    var relationList = otherTable.GetRelationsWhereChild();
                    if (relationList.Count() == 2)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Determines if this is a 1:1 relationship
        /// </summary>
        [Browsable(false)]
        public bool IsOneToOne
        {
            get
            {
                //If any of the columns are not unique then the relationship is NOT unique
                var retval = true;
                var childPKCount = 0; //Determine if any of the child columns are in the PK
                foreach (ColumnRelationship columnRelationship in this.ColumnRelationships)
                {
                    var column1 = columnRelationship.ParentColumn;
                    var column2 = columnRelationship.ChildColumn;
                    retval &= column1.IsUnique;
                    retval &= column2.IsUnique;
                    if (this.ChildTable.PrimaryKeyColumns.Contains(column2)) childPKCount++;
                }

                //If at least one column was a Child table PK, 
                //then all columns must be in there to be 1:1
                if ((childPKCount > 0) && (this.ColumnRelationships.Count != this.ChildTable.PrimaryKeyColumns.Count))
                {
                    return false;
                }

                return retval;
            }
        }

        /// <summary>
        /// Determines if all fields on both sides of the relation are table PKs
        /// </summary>
        [Browsable(false)]
        public bool AreAllFieldsPK
        {
            get
            {
                if ((this.ParentTable == null) || (this.ChildTable == null)) return false;
                if (this.ParentTable.PrimaryKeyColumns.Count != this.ChildTable.PrimaryKeyColumns.Count) return false;

                foreach (ColumnRelationship columnRelationship in this.ColumnRelationships)
                {
                    if ((columnRelationship.ParentColumn == null) || (!columnRelationship.ParentColumn.PrimaryKey)) return false;
                    if ((columnRelationship.ChildColumn == null) || (!columnRelationship.ChildColumn.PrimaryKey)) return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Determines if this is an inheritance relationship
        /// </summary>
        [Browsable(false)]
        public bool IsInherited
        {
            get
            {
                if (!this.IsOneToOne) return false;
                var parentTable = this.ParentTable;
                var childTable = this.ChildTable;
                return childTable.IsInheritedFrom(parentTable);
            }
        }

        [Browsable(false)]
        [Description("Determines if this relation is enforced in the database.")]
        [Category("Data")]
        [DefaultValue(_def_enforce)]
        public bool Enforce
        {
            get { return _enforce; }
            set
            {
                if (_enforce != value)
                {
                    _enforce = value;
                    this.OnPropertyChanged(this, new PropertyChangedEventArgs("Enforce"));
                }
            }
        }

        [Browsable(false)]
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                this.OnPropertyChanged(this, new PropertyChangedEventArgs("Description"));
            }
        }

        [Browsable(false)]
        public DeleteActionConstants DeleteAction
        {
            get { return _deleteAction; }
            set
            {
                _deleteAction = value;
                this.OnPropertyChanged(this, new PropertyChangedEventArgs("DeleteAction"));
            }
        }

        /// <summary>
        /// A hash of the table/columns of this relationship with no role information
        /// </summary>
        [Browsable(false)]
        public string LinkHash
        {
            get
            {
                var retval = string.Empty;
                if (this.ParentTable != null) retval += this.ParentTable.Name.ToLower() + "|";
                if (this.ChildTable != null) retval += this.ChildTable.Name.ToLower() + "|";
                foreach (var cr in this.ColumnRelationships.ToList())
                {
                    if (cr.ParentColumn != null) retval += cr.ParentColumn.Name.ToLower() + "|";
                    if (cr.ChildColumn != null) retval += cr.ChildColumn.Name.ToLower() + "|";
                }
                return retval;
            }
        }

        public int UniqueHash
        {
            get { return (this.LinkHash + "|" + this.RoleName).GetHashCode(); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines that all columns in the relationship are generated
        /// </summary>
        public bool IsGenerated
        {
            get
            {
                var retval = true;
                foreach (ColumnRelationship columnRelationship in this.ColumnRelationships)
                {
                    var childColumn = columnRelationship.ChildColumn;
                    var parentColumn = columnRelationship.ParentColumn;
                    retval &= childColumn.Generated;
                    retval &= parentColumn.Generated;
                }
                return retval;
            }
        }

        public bool IsPrimaryKeyRelation()
        {
            //Determine if this relation ship is based on primary keys
            var retval = true;
            foreach (ColumnRelationship columnRelationship in this.ColumnRelationships)
            {
                var parentColumn = columnRelationship.ParentColumn;
                var parentTable = this.ParentTable;
                if (!parentTable.PrimaryKeyColumns.Contains(parentColumn))
                    retval = false;
            }
            return retval;
        }

        public string ToLongString()
        {
            try
            {
                var col1 = this.ColumnRelationships.First().ParentColumn;
                var col2 = this.ColumnRelationships.First().ChildColumn;
                var retval = (this.RoleName == "" ? "" : this.RoleName + " ");
                retval += ((Table)col1.ParentTableRef.Object).Name + ".";
                retval += col1.ToString();
                retval += "->";
                retval += ((Table)col2.ParentTableRef.Object).Name + ".";
                retval += col2.ToString();
                return retval;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool IsInvalidRelation()
        {
            if (this.ChildTableRef == null) return true;
            if (this.ChildTableRef.Object == null) return true;
            if (this.ParentTableRef == null) return true;
            if (this.ParentTableRef.Object == null) return true;
            return false;
        }

        public override bool Equals(object obj)
        {
            try
            {
                if (!(obj is Relation)) return false;
                var relationOther = (Relation)obj;

                if (this.IsInvalidRelation()) return false;
                if (relationOther.IsInvalidRelation()) return false;

                #region Check Parents
                var parentTableName1 = ((Table)this.ParentTableRef.Object).Name;
                var parentTableName2 = ((Table)relationOther.ParentTableRef.Object).Name;

                var list1 = new SortedDictionary<string, ColumnRelationship>();
                foreach (ColumnRelationship cr in this.ColumnRelationships)
                {
                    if (cr.ChildColumnRef.Object != null)
                    {
                        var column = (Column)cr.ChildColumnRef.Object;
                        if (!list1.ContainsKey(column.Name))
                            list1.Add(column.Name, cr);
                    }
                }

                var list2 = new SortedDictionary<string, ColumnRelationship>();
                foreach (ColumnRelationship cr in relationOther.ColumnRelationships)
                {
                    if (cr.ChildColumnRef.Object != null)
                    {
                        var column = (Column)cr.ChildColumnRef.Object;
                        if (!list2.ContainsKey(column.Name))
                            list2.Add(((Column)cr.ChildColumnRef.Object).Name, cr);
                    }
                }

                var parentColName1 = string.Empty;
                foreach (var key in list1.Keys)
                {
                    parentColName1 += key;
                }

                var parentColName2 = string.Empty;
                foreach (var key in list2.Keys)
                {
                    parentColName2 += key;
                }
                #endregion

                #region Check Children
                var childTableName1 = ((Table)this.ChildTableRef.Object).Name;
                var childTableName2 = ((Table)relationOther.ChildTableRef.Object).Name;

                var list3 = new SortedDictionary<string, ColumnRelationship>();
                foreach (ColumnRelationship cr in this.ColumnRelationships)
                {
                    if (cr.ParentColumnRef.Object != null)
                    {
                        var column = (Column)cr.ParentColumnRef.Object;
                        if (!list3.ContainsKey(column.Name))
                            list3.Add(column.Name, cr);
                    }
                }

                var list4 = new SortedDictionary<string, ColumnRelationship>();
                foreach (ColumnRelationship cr in relationOther.ColumnRelationships)
                {
                    if (cr.ParentColumnRef.Object != null)
                    {
                        var column = (Column)cr.ParentColumnRef.Object;
                        if (!list4.ContainsKey(column.Name))
                            list4.Add(column.Name, cr);
                    }
                }

                var childColName1 = string.Empty;
                foreach (var key in list3.Keys)
                {
                    childColName1 += key;
                }

                var childColName2 = string.Empty;
                foreach (var key in list4.Keys)
                {
                    childColName2 += key;
                }
                #endregion

                //string parentCol
                return (parentTableName1 == parentTableName2) &&
                    (parentColName1 == parentColName2) &&
                    (childTableName1 == childTableName2) &&
                    (childColName1 == childColName2);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Get the parent table of this relation
        /// </summary>
        /// <returns></returns>
        public Table ParentTable
        {
            get
            {
                if (this.ParentTableRef == null) return null;
                if (this.ParentTableRef.Object == null) return null;
                return this.ParentTableRef.Object as Table;
            }
        }

        /// <summary>
        /// Get the child table of this relation
        /// </summary>
        /// <returns></returns>
        public Table ChildTable
        {
            get
            {
                if (this.ChildTableRef == null) return null;
                if (this.ChildTableRef.Object == null) return null;
                return this.ChildTableRef.Object as Table;
            }
        }

        public Table GetSecondaryAssociativeTable()
        {
            if (!this.IsManyToMany) return null;

            var parentTable = (Table)this.ParentTableRef.Object;
            var childTable = (Table)this.ChildTableRef.Object;

            var otherTable = parentTable;
            if (childTable.AssociativeTable) otherTable = childTable;

            if (otherTable.AssociativeTable)
            {
                var relationList = otherTable.GetRelationsWhereChild();
                {
                    var relation = relationList.Where(x => x != this).FirstOrDefault();
                    if (relation == null) return null;

                    return relation.ParentTableRef.Object as Table;

                }
            }
            return null;

        }

        public Relation GetAssociativeOtherRelation()
        {
            if (!this.IsManyToMany) return null;

            var parentTable = (Table)this.ParentTableRef.Object;
            var childTable = (Table)this.ChildTableRef.Object;

            var otherTable = parentTable;
            if (childTable.AssociativeTable) otherTable = childTable;

            if (otherTable.AssociativeTable)
            {
                var relationList = otherTable.GetRelationsWhereChild();
                if (relationList.Count() == 2)
                {
                    var relation = relationList.Where(x => x != this).FirstOrDefault();
                    if (relation == null) return null;
                    return relation;
                }
            }
            return null;
        }

        #endregion

        public string CorePropertiesHash
        {
            get
            {
                var sb = new StringBuilder();
                this.ColumnRelationships.ToList().ForEach(x => sb.Append(x.CorePropertiesHash));

                var prehash =
                    this.RoleName + "|" +
                    sb.ToString();
                //return HashHelper.Hash(prehash);
                return prehash;
            }
        }

        #region IXMLable Members

        public override void XmlAppend(XmlNode node)
        {
            var oDoc = node.OwnerDocument;

            XmlHelper.AddAttribute(node, "key", this.Key);
            XmlHelper.AddAttribute(node, "enforce", this.Enforce);

            if (this.Description != _def_description)
                XmlHelper.AddAttribute(node, "description", this.Description);

            XmlHelper.AddAttribute(node, "deleteAction", this.DeleteAction.ToString());

            var columnRelationshipsNode = oDoc.CreateElement("crl");
            ColumnRelationships.XmlAppend(columnRelationshipsNode);
            node.AppendChild(columnRelationshipsNode);

            var childTableRefNode = oDoc.CreateElement("ct");
            if (this.ChildTableRef != null)
                this.ChildTableRef.XmlAppend(childTableRefNode);
            node.AppendChild(childTableRefNode);

            var parentTableRefNode = oDoc.CreateElement("pt");
            if (this.ParentTableRef != null)
                this.ParentTableRef.XmlAppend(parentTableRefNode);
            node.AppendChild(parentTableRefNode);

            XmlHelper.AddAttribute(node, "id", this.Id);
            if (this.RoleName != _def_roleName)
                XmlHelper.AddAttribute(node, "roleName", this.RoleName);
            if (this.ConstraintName != _def_constraintname)
                XmlHelper.AddAttribute(node, "constraintName", this.ConstraintName);
        }

        public override void XmlLoad(XmlNode node)
        {
            this.Key = XmlHelper.GetAttributeValue(node, "key", string.Empty);
            _enforce = XmlHelper.GetAttributeValue(node, "enforce", _def_enforce);
            _description = XmlHelper.GetAttributeValue(node, "description", _def_description);

            _deleteAction = (DeleteActionConstants)Enum.Parse(typeof(DeleteActionConstants), XmlHelper.GetAttributeValue(node, "deleteAction", _def_deleteAction.ToString()));

            var columnRelationshipsNode = node.SelectSingleNode("columnRelationships"); //deprecated, use "crl"
            if (columnRelationshipsNode == null)
                columnRelationshipsNode = node.SelectSingleNode("crl");
            ColumnRelationships.XmlLoad(columnRelationshipsNode);

            var childTableRefNode = node.SelectSingleNode("childTableRef"); //deprecated, use "ct"
            if (childTableRefNode == null) childTableRefNode = node.SelectSingleNode("ct");
            if (this.ChildTableRef == null) _childTableRef = new Reference(this.Root);
            this.ChildTableRef.XmlLoad(childTableRefNode);

            var parentTableRefNode = node.SelectSingleNode("parentTableRef"); //deprecated, use "pt"
            if (parentTableRefNode == null) parentTableRefNode = node.SelectSingleNode("pt");
            if (this.ParentTableRef == null) _parentTableRef = new Reference(this.Root);
            this.ParentTableRef.XmlLoad(parentTableRefNode);

            this.ResetId(XmlHelper.GetAttributeValue(node, "id", this.Id));

            var roleName = XmlHelper.GetAttributeValue(node, "roleName", _def_roleName);
            if (roleName == "fk") roleName = string.Empty; //Error correct from earlier versions
            this.RoleName = roleName;

            this.ConstraintName = XmlHelper.GetAttributeValue(node, "constraintName", _def_constraintname);
            //_createdDate = DateTime.ParseExact(XmlHelper.GetAttributeValue(node, "createdDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

            this.Dirty = false;
        }

        #endregion

        #region Helpers

        public Reference CreateRef()
        {
            return CreateRef(Guid.NewGuid().ToString());
        }

        public Reference CreateRef(string key)
        {
            var returnVal = new Reference(this.Root);
            returnVal.ResetKey(key);
            returnVal.Ref = this.Id;
            returnVal.RefType = ReferenceType.Relation;
            return returnVal;
        }

        [Browsable(false)]
        public string PascalRoleName
        {
            get
            {
                if (((ModelRoot)this.Root).TransformNames)
                    return StringHelper.DatabaseNameToPascalCase(RoleName);
                else
                    return StringHelper.FirstCharToUpper(this.RoleName);
            }
        }

        [Browsable(false)]
        public string CamelRoleName
        {
            get
            {
                if (((ModelRoot)this.Root).TransformNames)
                    return StringHelper.DatabaseNameToCamelCase(RoleName);
                else
                    return StringHelper.FirstCharToLower(this.RoleName);
            }
        }

        [Browsable(false)]
        public string DatabaseRoleName
        {
            get { return this.RoleName; }
        }

        [Browsable(false)]
        public IEnumerable<Column> FkColumns
        {
            get
            {
                try
                {
                    var sorted = new SortedDictionary<string, Column>();
                    foreach (ColumnRelationship columnRel in this.ColumnRelationships)
                    {
                        var parentColumn = columnRel.ParentColumn;
                        var childColumn = columnRel.ChildColumn;
                        sorted.Add(parentColumn.Name + "|" + childColumn.Name + "|" + this.RoleName + "|" + columnRel.Key, childColumn);
                    }

                    var fkColumns = new List<Column>();
                    foreach (var kvp in sorted)
                    {
                        fkColumns.Add(kvp.Value);
                    }
                    return fkColumns;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        public override string ToString()
        {
            var tableCollection = ((ModelRoot)this.Root).Database.Tables;
            Table[] parentList = { };
            Table[] childList = { };
            if (this.ParentTableRef != null)
                parentList = tableCollection.GetById(this.ParentTableRef.Ref);
            if (this.ChildTableRef != null)
                childList = tableCollection.GetById(this.ChildTableRef.Ref);

            var retval = string.Empty;
            retval = (this.RoleName == "" ? "" : this.RoleName + " ") + "[" + ((parentList.Length == 0) ? "(Unknown)" : parentList[0].Name) + " -> " + ((childList.Length == 0) ? "(Unknown)" : childList[0].Name) + "]";
            return retval;
        }

        private void RefreshRoleName()
        {
            //try
            //{
            //  string newRoleName = string.Empty;
            //  if ((this.ParentTableRef != null) && (this.ChildTableRef != null))
            //  {
            //    newRoleName = ((Table)this.ParentTableRef.Object).Name + "_" + ((Table)this.ChildTableRef.Object).Name;
            //    Database database = ((ModelRoot)this.Root).Database;
            //    if (database.RelationRoleExists(newRoleName, this))
            //    {
            //      //If we are in there then need to loop and find a new name
            //      int ii = 1;
            //      while (database.RelationRoleExists(newRoleName + ii.ToString(), this))
            //        ii++;
            //      newRoleName = newRoleName + ii.ToString();
            //    }
            //  }
            //  this.RoleName = newRoleName;
            //}
            //catch (Exception ex)
            //{
            //  throw;
            //}
        }

        #endregion

    }
}