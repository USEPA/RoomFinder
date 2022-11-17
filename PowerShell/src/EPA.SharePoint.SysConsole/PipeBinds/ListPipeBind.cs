using Microsoft.SharePoint.Client;
using System;
using System.Linq.Expressions;

namespace EPA.SharePoint.SysConsole.PipeBinds
{
    public sealed class ListPipeBind
    {
        private readonly Guid _id;

        public ListPipeBind()
        {
            List = null;
            _id = Guid.Empty;
            Title = string.Empty;
        }

        public ListPipeBind(List list)
        {
            List = list;
        }

        public ListPipeBind(Guid guid)
        {
            _id = guid;
        }

        public ListPipeBind(string id)
        {
            if (!Guid.TryParse(id, out _id))
            {
                Title = id;
            }
        }

        public Guid Id
        {
            get { return _id; }
        }

        public List List { get; }

        public string Title { get; }
    }
}
