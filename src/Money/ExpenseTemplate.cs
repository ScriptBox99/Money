﻿using Money.Events;
using Neptuo;
using Neptuo.Events;
using Neptuo.Events.Handlers;
using Neptuo.Models.Domains;
using Neptuo.Models.Keys;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Money
{
    /// <summary>
    /// Model of expense template.
    /// </summary>
    public class ExpenseTemplate : AggregateRoot,
        IEventHandler<ExpenseTemplateCreated>,
        IEventHandler<ExpenseTemplateAmountChanged>,
        IEventHandler<ExpenseTemplateDescriptionChanged>,
        IEventHandler<ExpenseTemplateCategoryChanged>,
        IEventHandler<ExpenseTemplateFixedChanged>,
        IEventHandler<ExpenseTemplateDeleted>
    {
        public bool IsDeleted { get; private set; }

        /// <summary>
        /// Gets an amount of the expense template.
        /// </summary>
        public Price Amount { get; private set; }

        /// <summary>
        /// Gets a description of the expense template.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets a category of the expense template.
        /// </summary>
        public IKey CategoryKey { get; private set; }

        /// <summary>
        /// Gets whether the template should create fixed expenses.
        /// </summary>
        public bool IsFixed { get; private set; }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="amount">An amount of the expense template.</param>
        /// <param name="description">A description of the expense template.</param>
        /// <param name="categoryKey">A category of the expense template.</param>
        public ExpenseTemplate(Price amount, string description, IKey categoryKey)
        {
            Ensure.NotNull(categoryKey, "categoryKey");
            Publish(new ExpenseTemplateCreated(amount, description, categoryKey));
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="amount">An amount of the expense template.</param>
        /// <param name="description">A description of the expense template.</param>
        /// <param name="categoryKey">A category of the expense template.</param>
        public ExpenseTemplate(Price amount, string description, IKey categoryKey, bool isFixed)
        {
            Ensure.NotNull(categoryKey, "categoryKey");
            Publish(new ExpenseTemplateCreated(amount, description, categoryKey, isFixed));
        }

        public ExpenseTemplate(IKey key, IEnumerable<IEvent> events)
            : base(key, events)
        { }

        Task IEventHandler<ExpenseTemplateCreated>.HandleAsync(ExpenseTemplateCreated payload) => UpdateState(() =>
        {
            Amount = payload.Amount;
            Description = payload.Description;
            CategoryKey = payload.CategoryKey;
            IsFixed = payload.IsFixed;
        });

        private void EnsureNotDeleted()
        {
            if (IsDeleted)
                throw new ExpenseTemplateAlreadyDeletedException();
        }

        public void Delete()
        {
            EnsureNotDeleted();
            Publish(new ExpenseTemplateDeleted());
        }

        Task IEventHandler<ExpenseTemplateDeleted>.HandleAsync(ExpenseTemplateDeleted payload) => UpdateState(() =>
        {
            IsDeleted = true;
        });

        public void ChangeAmount(Price amount)
        {
            EnsureNotDeleted();
            Publish(new ExpenseTemplateAmountChanged(Amount, amount));
        }

        Task IEventHandler<ExpenseTemplateAmountChanged>.HandleAsync(ExpenseTemplateAmountChanged payload) => UpdateState(() =>
        {
            Amount = payload.NewValue;
        });

        public void ChangeDescription(string description)
        {
            EnsureNotDeleted();
            Publish(new ExpenseTemplateDescriptionChanged(description));
        }

        Task IEventHandler<ExpenseTemplateDescriptionChanged>.HandleAsync(ExpenseTemplateDescriptionChanged payload) => UpdateState(() =>
        {
            Description = payload.Description;
        });

        public void ChangeCategory(IKey categoryKey)
        {
            Ensure.NotNull(categoryKey, "categoryKey");
            EnsureNotDeleted();
            Publish(new ExpenseTemplateCategoryChanged(categoryKey));
        }

        Task IEventHandler<ExpenseTemplateCategoryChanged>.HandleAsync(ExpenseTemplateCategoryChanged payload) => UpdateState(() =>
        {
            CategoryKey = payload.CategoryKey;
        });

        public void ChangeFixed(bool isFixed)
        {
            EnsureNotDeleted();
            if (isFixed == IsFixed)
                throw new ExpenseTemplateAlreadyFixedException();

            Publish(new ExpenseTemplateFixedChanged(isFixed));
        }

        Task IEventHandler<ExpenseTemplateFixedChanged>.HandleAsync(ExpenseTemplateFixedChanged payload) => UpdateState(() =>
        {
            IsFixed = payload.IsFixed;
        });
    }
}
