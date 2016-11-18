﻿using Money.ViewModels.Commands;
using Neptuo.Observables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Money.ViewModels
{
    /// <summary>
    /// A base view model for view with posibility to create out/in comes.
    /// </summary>
    public class ViewModel : ObservableObject
    {
        public ICommand CreateIncome { get; private set; }
        public ICommand CreateOutcome { get; private set; }

        public ViewModel()
        {
            CreateOutcome = new CreateOutcomeCommand();
        }
    }
}
