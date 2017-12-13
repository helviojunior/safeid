using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SafeTrend.Json;


[Serializable()]
public class AutoCompleteItem
{
    public Int64 id;
    public String label;
    public String html;

    public AutoCompleteItem(Int64 id, String label, String html)
    {
        this.id = id;
        this.label = label;
        this.html = html;
    }
}




