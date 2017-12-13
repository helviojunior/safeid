using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Collections.Specialized;


public class HashData: IDisposable
{
    private Dictionary<String, String> keyValues = new Dictionary<String, String>();

    public HashData(Page page)
    {
        {
            if (page.Request.Form["hashtag"] != null)
                Add(page.Request.Form["hashtag"].ToString().ToLower().TrimStart("#/ ".ToCharArray()).Split("/".ToCharArray()));
        }
    }

    private void Add(String[] inputs)
    {
        Int32 numberPairs = inputs.Length / 2;

        for (Int32 i = 0; i < numberPairs; i++)
        {
            String key = (String)inputs[2 * i];
            String value = (String)inputs[2 * i + 1];

            if (!keyValues.ContainsKey(key))
                keyValues.Add(key, value);

        }

    }

    public String GetValue(String key)
    {
        if (keyValues.ContainsKey(key))
            return keyValues[key];
        else
            return "";
    }

    public void Clear()
    {
        keyValues.Clear();
    }

    public void Dispose()
    {
        keyValues.Clear();
        keyValues = null;

    }
}
