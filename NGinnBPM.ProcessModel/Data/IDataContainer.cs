using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace NGinnBPM.ProcessModel.Data
{
    /// <summary>
    /// Experimental, not official
    /// Universal data container for task and process data.
    /// </summary>
    public interface IDataContainer
    {
        IList<string> FieldNames { get; }
        void SetField(string name, object value);
        object GetField(string name);
        bool HasField(string name);
        void CopyFrom(IDataContainer container);
        void CopyFrom(IDictionary<string, object> dic);
        void CopyFrom(IDictionary dic);
        IDictionary<string, object> ToGenericDictionary();
        IDictionary ToDictionary();
        int GetInt(string name);
        double GetDouble(string name);
        string GetString(string name);
        DateTime GetDateTime(string name);
        bool GetBool(string name);
        void SetTypeDefinition(StructDef sd);
        StructDef GetTypeDefinition();
        bool IsArray(string fieldName);
        bool IsStruct(string fieldName);
        bool ValidateAgainst(StructDef sd, TypeSet ts);
        string ToJson();
        void LoadJson(string s);
    }
}
