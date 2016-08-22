using System;
using System.Diagnostics;
using System.Windows.Input;

namespace StructuredXmlEditor.View
{
	//-----------------------------------------------------------------------
	public class Command<T> : ICommand
	{
		//##########################################################################
		#region Constructor

		//-----------------------------------------------------------------------
		public Command
		(
			Action<T> execute
		)
			: this(execute, null)
		{
			
		}

		//-----------------------------------------------------------------------
		public Command
		(
			Action<T> execute,
			Predicate<T> canExecute
		)
		{
			m_execute = execute;
			m_canExecute = canExecute;
		}

		#endregion Constructor
		//##########################################################################
		#region Methods

		//-----------------------------------------------------------------------
		public bool CanExecute
		(
			Object _parameter
		)
		{
			return m_canExecute == null ? true : m_canExecute((T)_parameter);
		}

		//-----------------------------------------------------------------------
		public void Execute
		(
			Object _parameter
		)
		{
			if (m_execute != null)
			{
				m_execute((T)_parameter);
			}
		}

		#endregion Methods
		//##########################################################################
		#region Events

		//-----------------------------------------------------------------------
		public event EventHandler CanExecuteChanged
		{
			add
			{
				CommandManager.RequerySuggested += value;
			}
			remove
			{
				CommandManager.RequerySuggested -= value;
			}
		}

		#endregion Events
		//##########################################################################
		#region Data

		protected readonly Action<T> m_execute;
		protected readonly Predicate<T> m_canExecute;

		#endregion Data
		//##########################################################################
	}
}