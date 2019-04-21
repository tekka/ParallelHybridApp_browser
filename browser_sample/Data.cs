using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParallelHybridApp
{

    public class MessageData
    {
        public string command { get; set; }
        public string message { get; set; }
        public string time { get; set; }
    }

    public class SearchCompleteDataResult
    {
        public string title { get; set; }
        public string href { get; set; }
    }

    public class SearchCompleteData
    {
        public string command { get { return "search_completed"; } }
        public List<SearchCompleteDataResult> search_result_ary { get; set; }
        public string time { get; set; }
    }

}
