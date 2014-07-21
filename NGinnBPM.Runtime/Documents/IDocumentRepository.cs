using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.Documents
{
    public interface IDocumentSessionFactory
    {
        IDocumentRepositorySession OpenSession();
    }

    public interface IDocumentRepositorySession : IDisposable
    {
        object GetDocument(string docref);
        string SaveNewDocument(object doc);
        void DocumentUpdated(object doc);
        void SaveChanges();
    }
}
