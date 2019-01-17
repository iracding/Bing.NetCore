﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Bing.Datas.Sql.Queries.Builders.Abstractions;
using Bing.Datas.Sql.Queries.Builders.Core;

namespace Bing.Datas.Sql.Queries.Builders.Clauses
{
    /// <summary>
    /// Select子句
    /// </summary>
    public class SelectClause:ISelectClause
    {
        /// <summary>
        /// Sql生成器
        /// </summary>
        private readonly ISqlBuilder _sqlBuilder;

        /// <summary>
        /// Sql方言
        /// </summary>
        private readonly IDialect _dialect;

        /// <summary>
        /// 实体解析器
        /// </summary>
        private readonly IEntityResolver _resolver;

        /// <summary>
        /// 实体别名注册器
        /// </summary>
        private readonly IEntityAliasRegister _register;

        /// <summary>
        /// 列名集合
        /// </summary>
        private readonly List<ColumnCollection> _columns;

        /// <summary>
        /// 初始化一个<see cref="SelectClause"/>类型的实例
        /// </summary>
        /// <param name="sqlBuilder">Sql生成器</param>
        /// <param name="dialect">Sql方言</param>
        /// <param name="resolver">实体解析器</param>
        /// <param name="register">实体别名注册器</param>
        public SelectClause(ISqlBuilder sqlBuilder, IDialect dialect, IEntityResolver resolver,
            IEntityAliasRegister register)
        {
            _columns = new List<ColumnCollection>();
            _sqlBuilder = sqlBuilder;
            _dialect = dialect;
            _resolver = resolver;
            _register = register;
        }

        /// <summary>
        /// 初始化一个<see cref="SelectClause"/>类型的实例
        /// </summary>
        /// <param name="selectClause">Select子句</param>
        /// <param name="sqlBuilder">Sql生成器</param>
        /// <param name="register">实体别名生成器</param>
        protected SelectClause(SelectClause selectClause, ISqlBuilder sqlBuilder, IEntityAliasRegister register)
        {
            _sqlBuilder = sqlBuilder;
            _register = register;
            _dialect = selectClause._dialect;
            _resolver = selectClause._resolver;
            _columns = new List<ColumnCollection>(selectClause._columns);
        }

        /// <summary>
        /// 克隆
        /// </summary>
        /// <param name="sqlBuilder">Sql生成器</param>
        /// <param name="register">实体别名注册器</param>
        /// <returns></returns>
        public virtual ISelectClause Clone(ISqlBuilder sqlBuilder, IEntityAliasRegister register)
        {
            return new SelectClause(this, sqlBuilder, register);
        }

        /// <summary>
        /// 设置列名
        /// </summary>
        /// <param name="columns">列名</param>
        /// <param name="tableAlias">表别名</param>
        public void Select(string columns, string tableAlias = null)
        {
            if (string.IsNullOrWhiteSpace(columns))
            {
                return;
            }
            _columns.Add(new ColumnCollection(columns, tableAlias));
        }

        /// <summary>
        /// 设置列名
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="expression">列名表达式</param>
        /// <param name="propertyAsAlias">是否将属性名映射为列别名</param>
        public void Select<TEntity>(Expression<Func<TEntity, object[]>> expression, bool propertyAsAlias = false) where TEntity : class
        {
            if (expression == null)
            {
                return;
            }

            _columns.Add(new ColumnCollection(_resolver.GetColumns(expression, propertyAsAlias), table: typeof(TEntity)));
        }

        /// <summary>
        /// 设置列名
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="expression">列名表达式</param>
        /// <param name="columnAlias">列别名</param>
        public void Select<TEntity>(Expression<Func<TEntity, object>> expression, string columnAlias = null) where TEntity : class
        {
            if (expression == null)
            {
                return;
            }

            var column = _resolver.GetColumn(expression);
            if (column.Contains("As") == false && string.IsNullOrWhiteSpace(columnAlias) == false)
            {
                column += $" As {columnAlias}";
            }

            _columns.Add(new ColumnCollection(column, table: typeof(TEntity)));
        }

        /// <summary>
        /// 添加到Select子句
        /// </summary>
        /// <param name="sql">Sql语句</param>
        public void AppendSql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return;
            }
            _columns.Add(new ColumnCollection(sql, raw: true));
        }

        /// <summary>
        /// 添加到Select子句
        /// </summary>
        /// <param name="builder">Sql生成器</param>
        /// <param name="columnAlias">列别名</param>
        public void AppendSql(ISqlBuilder builder, string columnAlias)
        {
            if (builder == null)
            {
                return;
            }

            var result = builder.ToSql();
            if (string.IsNullOrWhiteSpace(columnAlias) == false)
            {
                result = $"({result}) As {_dialect.SafeName(columnAlias)}";
            }

            AppendSql(result);
        }

        /// <summary>
        /// 添加到Select子句
        /// </summary>
        /// <param name="action">子查询操作</param>
        /// <param name="columnAlias">列别名</param>
        public void AppendSql(Action<ISqlBuilder> action, string columnAlias)
        {
            if (action == null)
            {
                return;
            }

            var builder = _sqlBuilder.New();
            action(builder);
            AppendSql(builder, columnAlias);
        }

        /// <summary>
        /// 输出Sql
        /// </summary>
        /// <returns></returns>
        public string ToSql()
        {
            return $"Select {GetColumns()}";
        }

        /// <summary>
        /// 获取列名
        /// </summary>
        /// <returns></returns>
        protected virtual string GetColumns()
        {
            if (_columns.Count == 0)
            {
                return "*";
            }

            var result = string.Empty;
            foreach (var item in _columns)
            {
                result += item.ToSql(_dialect, _register);
                if (item.Raw == false)
                {
                    result += ",";
                }
            }

            return result.TrimEnd(',');
        }
    }
}
