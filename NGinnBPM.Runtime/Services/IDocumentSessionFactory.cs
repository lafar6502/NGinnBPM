using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.Services
{
    /// <summary>
    /// Document session factory
    /// </summary>
    public interface IDocumentSessionFactory
    {
        IDocumentSession OpenSession();
        IDocumentSession OpenSession(object connection);
    }

    /// <summary>
    /// Document session is responsible for accessing application's document repository.
    /// Application should provide an implementation if documents are to be modified
    /// by NGinn.BPM processes.
    /// TODO: there's no method for querying the document repository. Not sure if it's possible to add
    /// anything generic here.
    /// </summary>
    public interface IDocumentSession : IDisposable
    {
        /// <summary>
        /// Retrieve document for updating
        /// </summary>
        /// <param name="id"></param>
        /// <param name="forUpdate">set this to true if you intend to modify the document in current session</param>
        /// <returns></returns>
        object GetDocument(string id, bool forUpdate);
        /// <summary>
        /// Update document instance (call this after modifying a document)
        /// </summary>
        /// <param name="doc"></param>
        void UpdateDocument(object doc);
        /// <summary>
        /// Insert a new document instance
        /// </summary>
        /// <param name="doc"></param>
        void AddNewDocument(object doc);
        /// <summary>
        /// Write changes made in current session to the database
        /// </summary>
        void SaveChanges();
    }
}
