﻿using Rubberduck.UI.ToDoItems;

namespace Rubberduck.UI.Command
{
    public class ToDoExplorerCommand : ICommand
    {
        private readonly ToDoExplorerDockablePresenter _presenter;

        public ToDoExplorerCommand(ToDoExplorerDockablePresenter presenter)
        {
            _presenter = presenter;
        }

        public void Execute()
        {
            _presenter.Show();
        }
    }

    public class ToDoExplorerCommandMenuItem : CommandMenuItemBase
    {
        public ToDoExplorerCommandMenuItem(ICommand command) 
            : base(command)
        {
        }

        public override string Key { get { return "RubberduckMenu_ToDoItems"; } }
        public override int DisplayOrder { get { return (int)RubberduckMenuItemDisplayOrder.ToDoExplorer; } }
    }
}