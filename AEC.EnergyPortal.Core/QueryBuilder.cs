using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;

namespace AEC.EnergyPortal.Core
{
    public class QueryBuilder
    {
        #region Variables

        private List<Filter> Filters { get; set; }
        private List<OrderBy> SortOrders { get; set; }
        private List<Field> ViewFields { get; set; }
        private bool UseView;
        private string Scope;
        private QueryBuilder ViewQuery;
        
        private bool NeedsWhere = true;

        #endregion

        #region Constructors

        public QueryBuilder()
        {
            Filters = new List<Filter>();
            SortOrders = new List<OrderBy>();
            ViewFields = new List<Field>();
            UseView = false;
        }

        public QueryBuilder(bool StartWithWhere)
            : this()
        {
            NeedsWhere = StartWithWhere;
        }

        public QueryBuilder(QueryBuilder temp, bool cond, bool isElse)
            : this()
        {
            _temp = temp;
            _Condition = cond;
            _IsElse = isElse;
        }

        #endregion

        #region Operators


        public QueryBuilder AndBegin()
        {
            Filters.Add
                (
                        new Filter { Expression = "<And>" }
                );
            return this;
        }

        public QueryBuilder AndEnd()
        {
            Filters.Add
                (
                        new Filter { Expression = "</And>" }
                );
            return this;
        }

        public QueryBuilder OrBegin()
        {
            Filters.Add
                (
                        new Filter { Expression = "<Or>" }
                );
            return this;
        }

        public QueryBuilder OrEnd()
        {
            Filters.Add
                (
                        new Filter { Expression = "</Or>" }
                );
            return this;
        }

        #endregion

        #region Filters

        public QueryBuilder NullFilter(string name)
        {
            var query = "<IsNull><FieldRef Name='{0}' /></IsNull>";
            query = string.Format(query, name);
            Filters.Add
                (
                        new Filter { Expression = query }
                );
            return this;
        }

        public QueryBuilder EqualFilter(string name, string value)
        {
            return EqualFilter(name, value, "Text");
        }

        public QueryBuilder EqualFilter(string name, string value, string type)
        {
            var query = "<Eq><FieldRef Name='{0}' /><Value Type='{2}'>{1}</Value></Eq>";
            query = string.Format(query, name, value, type);
            Filters.Add
                (
                        new Filter { Expression = query }
                );
            return this;
        }

        public QueryBuilder NotEqualFilter(string name, string value)
        {
            return NotEqualFilter(name, value, "Text");
        }

        public QueryBuilder NotEqualFilter(string name, string value, string type)
        {
            var query = "<Neq><FieldRef Name='{0}' /><Value Type='{2}'>{1}</Value></Neq>";
            query = string.Format(query, name, value, type);
            Filters.Add
                (
                        new Filter { Expression = query }
                );
            return this;
        }

        public QueryBuilder ContainsFilter(string name, string value)
        {
            return ContainsFilter(name, value, "Text");
        }

        public QueryBuilder ContainsFilter(string name, string value, string type)
        {
            var query = "<Contains><FieldRef Name='{0}' /><Value Type='{2}'>{1}</Value></Contains>";
            query = string.Format(query, name, value, type);
            Filters.Add
                (
                        new Filter { Expression = query }
                );
            return this;
        }

        public QueryBuilder BeginsWithFilter(string name, string value)
        {
            var query = "<BeginsWith><FieldRef Name='{0}' /><Value Type='Text'>{1}</Value></BeginsWith>";
            query = string.Format(query, name, value);
            Filters.Add
                (
                        new Filter { Expression = query }
                );
            return this;
        }

        public QueryBuilder EqualLookupFilter(string name, string value)
        {
            var query = "<Eq><FieldRef Name='{0}' LookupId='TRUE' /><Value Type='Lookup'>{1}</Value></Eq>";
            query = string.Format(query, name, value);
            Filters.Add
                (
                        new Filter { Expression = query }
                );
            return this;
        }

        public QueryBuilder InFilter(string name, string[] values, string type)
        {
            var query = "<In><FieldRef LookupId='TRUE' Name='{0}' /><Values>{1}</Values></In>";
            query = string.Format(query, name, ValueFilters(values, type));
            Filters.Add
                (
                        new Filter { Expression = query }
                );
            return this;
        }

        public QueryBuilder InFilter(string name, IEnumerable<int> values)
        {
            var query = "<In><FieldRef LookupId='TRUE' Name='{0}' /><Values>{1}</Values></In>";
            query = string.Format(query, name, ValueFilters(values));
            Filters.Add
                (
                        new Filter { Expression = query }
                );
            return this;
        }

        private string ValueFilters(string[] values, string type)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string val in values)
            {
                sb.Append("<Value Type='" + type + "'>");
                sb.Append(val);
                sb.Append("</Value>");
            }
            return sb.ToString();
        }

        private string ValueFilters(IEnumerable<int> values)
        {
            StringBuilder sb = new StringBuilder();

            foreach (int val in values)
            {
                sb.Append("<Value Type='Integer'>");
                sb.Append(val.ToString());
                sb.Append("</Value>");
            }
            return sb.ToString();
        }

        #endregion

        #region Query Sections

        public QueryBuilder SortBy(string name, string sortOrder)
        {
            var query = "<FieldRef Name='{0}' Ascending='{1}' />";
            query = string.Format(query, name, sortOrder);
            SortOrders.Add
                (
                        new OrderBy { Expression = query }
                );
            return this;
        }

        public QueryBuilder CreateView(string scope)
        {
            this.UseView = true;
            this.Scope = scope;
            return this;
        }

        public QueryBuilder Query(QueryBuilder query)
        {
            this.ViewQuery = query;
            return this;
        }

        #endregion

        #region View Fields

        public QueryBuilder ViewField(string name)
        {
            var query = "<FieldRef Name='{0}' Nullable='TRUE' />";
            query = string.Format(query, name);
            ViewFields.Add
                (
                        new Field { Expression = query }
                );
            return this;
        }

        public QueryBuilder ViewField(string name, string fieldType)
        {
            var query = "<FieldRef Name='{0}' Type='{1}' Nullable='TRUE' />";
            query = string.Format(query, name, fieldType);
            ViewFields.Add
                (
                        new Field { Expression = query }
                );
            return this;
        }

        #endregion

        #region Insert Method

        public QueryBuilder Insert(QueryBuilder insert)
        {
            if (insert != null)
            {
                insert.NeedsWhere = false;  // You should have done this when creating... but just in case

                Filters.Add
                (
                        new Filter { Expression = insert.ToString() }
                );
            }
            return this;
        }

        #endregion

        #region Output Methods

        public SPQuery Build()
        {
            return Build(100);
        }

        public SPQuery Build(uint rowLimit)
        {
            var query = new SPQuery();

            if (UseView)
                query.ViewXml = this.ToString();
            else
                query.Query = this.ToString();

            query.RowLimit = rowLimit;
            return query;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (UseView)
                sb.AppendFormat("<View Scope='{0}'>", Scope);

            if (UseView && ViewQuery != null)
            {
                sb.Append("<Query>");
                sb.Append(ViewQuery.ToString());
                sb.Append("</Query>");
            }
            else
            {
                //Add Order By
                if (NeedsWhere && (Filters.Count > 0))
                    sb.Append("<Where>");

                foreach (var filter in Filters)
                {
                    sb.Append(filter.Expression);
                }

                if (NeedsWhere && (Filters.Count > 0))
                    sb.Append("</Where>");

                //Add Sort Order
                if (SortOrders.Count > 0)
                    sb.Append("<OrderBy>");

                foreach (var sortOrder in SortOrders)
                {
                    sb.Append(sortOrder.Expression);
                }

                if (SortOrders.Count > 0)
                    sb.Append("</OrderBy>");
            }

            if (UseView)
            {
                if (ViewFields.Count > 0)
                    sb.Append("<ViewFields>");

                foreach (var field in ViewFields)
                {
                    sb.Append(field.Expression);
                }

                if (ViewFields.Count > 0)
                    sb.Append("</ViewFields>");
            }

            if (UseView)
                sb.Append("</View>");

            return sb.ToString();
        }

        #endregion

        #region Conditional logic

        private QueryBuilder _temp;
        private bool _Condition = true;
        private bool _IsElse = false;

        //public QueryBuilder _IF_(bool cond)
        //{
        //    throw new Exception("_IF_ does not work correctly");

        //    _Condition = cond;

        //    if (cond) { return this; }
        //    else { return new QueryBuilder(this, cond, false); }
        //}

        //public QueryBuilder _ELSE_()
        //{
        //    throw new Exception("_ELSE_ does not work correctly");

        //    _IsElse = true;

        //    if (!_Condition) { _temp._IsElse = true;  return _temp; }
        //    else { return new QueryBuilder(this, _Condition, true); }
        //}

        //public QueryBuilder _ENDIF_(bool cond)
        //{
        //    throw new Exception("_ENDIF_ does not work correctly");

        //    if (_IsElse)
        //    {
        //        if (cond) { return _temp; }
        //        else { return this; }
        //    }
        //    else
        //    {
        //        if (cond) { return this; }
        //        else { return _temp; }
        //    }
        //}

        #endregion

        #region Internal classes

        private class Filter
        {
            public string Expression;
        }

        private class Field
        {
            public string Expression;
        }

        private class OrderBy
        {
            public string Expression;
        }

        public class ViewScope
        {
            public const string Recursive = "RecursiveAll";
        }

        public class FieldType
        {
            public const string Bool = "Boolean";
            public const string Text = "Text";
            public const string ModStat = "ModStat";
            public const string DateTime = "DateTime";
            public const string File = "File";
            public const string Integer = "Integer";
            public const string Choice = "Choice";
        }

        public class SortOrder
        {
            public const string Asc = "True";
            public const string Desc = "False";
        }

        #endregion

    }
}
