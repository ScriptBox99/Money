﻿using Microsoft.EntityFrameworkCore;
using Money.Data;
using Money.Events;
using Money.Services.Models.Queries;
using Neptuo;
using Neptuo.Activators;
using Neptuo.Events.Handlers;
using Neptuo.Models.Keys;
using Neptuo.Queries.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Money.Services.Models.Builders
{
    public class OutcomeBuilder : IEventHandler<OutcomeCreated>, 
        IEventHandler<OutcomeCategoryAdded>, 
        IQueryHandler<ListMonthWithOutcome, IEnumerable<MonthModel>>,
        IQueryHandler<ListMonthCategoryWithOutcome, IEnumerable<CategoryWithAmountModel>>,
        IQueryHandler<GetTotalMonthOutcome, Price>
    {
        private readonly IFactory<Price, decimal> priceFactory;

        public OutcomeBuilder(IFactory<Price, decimal> priceFactory)
        {
            Ensure.NotNull(priceFactory, "priceFactory");
            this.priceFactory = priceFactory;
        }

        public Task<IEnumerable<MonthModel>> HandleAsync(ListMonthWithOutcome query)
        {
            return Task.FromResult<IEnumerable<MonthModel>>(new List<MonthModel>()
            {
                new MonthModel(2016, 9),
                new MonthModel(2016, 10),
                new MonthModel(2016, 11)
            });
        }

        public async Task<IEnumerable<CategoryWithAmountModel>> HandleAsync(ListMonthCategoryWithOutcome query)
        {
            using (ReadModelContext db = new ReadModelContext())
            {
                Dictionary<Guid, Price> totals = new Dictionary<Guid, Price>();

                List<OutcomeEntity> outcomes = await db.Outcomes
                    .Where(o => o.When.Month == query.Month.Month && o.When.Year == query.Month.Year)
                    .Include(o => o.Categories)
                    .ToListAsync();

                foreach (OutcomeEntity outcome in outcomes)
                {
                    foreach (OutcomeCategoryEntity category in outcome.Categories)
                    {
                        Price price;
                        if (totals.TryGetValue(category.CategoryId, out price))
                            price = price + new Price(outcome.Amount, outcome.Currency);
                        else
                            price = new Price(outcome.Amount, outcome.Currency);

                        totals[category.CategoryId] = price;
                    }
                }

                List<CategoryWithAmountModel> result = new List<CategoryWithAmountModel>();
                foreach (var item in totals)
                {
                    CategoryModel model = (await db.Categories.FindAsync(item.Key)).ToModel();
                    result.Add(new CategoryWithAmountModel(model.Key, model.Name, model.Color, item.Value));
                }

                return result;
            }
        }

        public async Task<Price> HandleAsync(GetTotalMonthOutcome query)
        {
            using (ReadModelContext db = new ReadModelContext())
            {
                List<Price> outcomes = await db.Outcomes
                    .Where(o => o.When.Month == query.Month.Month && o.When.Year == query.Month.Year)
                    .Select(o => new Price(o.Amount, o.Currency))
                    .ToListAsync();

                Price price = priceFactory.Create(0);
                foreach (Price outcome in outcomes)
                    price += outcome;

                return price;
            }
        }

        public async Task HandleAsync(OutcomeCategoryAdded payload)
        {
            using (ReadModelContext db = new ReadModelContext())
            {
                OutcomeEntity entity = await db.Outcomes.FindAsync(payload.AggregateKey.AsGuidKey().Guid);
                if (entity != null)
                {
                    entity.Categories.Add(new OutcomeCategoryEntity()
                    {
                        OutcomeId = payload.AggregateKey.AsGuidKey().Guid,
                        CategoryId = payload.CategoryKey.AsGuidKey().Guid
                    });
                    await db.SaveChangesAsync();
                }
            }
        }

        public Task HandleAsync(OutcomeCreated payload)
        {
            using (ReadModelContext db = new ReadModelContext())
            {
                db.Outcomes.Add(new OutcomeEntity(new OutcomeModel(
                    payload.AggregateKey,
                    payload.Amount,
                    payload.When,
                    payload.Description,
                    new List<IKey>() { payload.CategoryKey }
                )));
                return db.SaveChangesAsync();
            }
        }
    }
}
