﻿using Neptuo;
using Neptuo.Observables.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Specialized;
using Money.Services;
using Neptuo.Models.Keys;
using Money.Views.Navigation;

namespace Money.ViewModels.Commands
{
    public class SaveOutcomeCommand : NavigateBackCommand
    {
        private readonly OutcomeViewModel viewModel;
        private readonly IDomainFacade domainFacade;

        public SaveOutcomeCommand(INavigator navigator, OutcomeViewModel viewModel, IDomainFacade domainFacade)
            : base(navigator)
        {
            Ensure.NotNull(viewModel, "viewModel");
            Ensure.NotNull(domainFacade, "domainFacade");
            this.viewModel = viewModel;
            this.viewModel.PropertyChanged += OnViewModelPropertyChanged;
            this.viewModel.SelectedCategories.CollectionChanged += OnViewModelSelectedCategoriesChanged;
            this.domainFacade = domainFacade;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OutcomeViewModel.Amount))
                RaiseCanExecuteChanged();
        }

        private void OnViewModelSelectedCategoriesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaiseCanExecuteChanged();
        }

        public override bool CanExecute()
        {
            return viewModel.Amount > 0 && viewModel.SelectedCategories.Count > 0;
        }

        public override async void Execute()
        {
            IKey outcomeKey = await domainFacade.CreateOutcomeAsync(
                domainFacade.PriceFactory.Create((decimal)viewModel.Amount),
                viewModel.Description,
                viewModel.When,
                viewModel.SelectedCategories.First()
            );

            if (viewModel.SelectedCategories.Count > 1)
            {
                for (int i = 2; i < viewModel.SelectedCategories.Count; i++)
                {
                    IKey categoryKey = viewModel.SelectedCategories[i];
                    await domainFacade.AddOutcomeCategoryAsync(outcomeKey, categoryKey);
                }
            }

            viewModel.Amount = 0;
            viewModel.Description = null;
            viewModel.When = DateTime.Now;

            base.Execute();
        }
    }
}
