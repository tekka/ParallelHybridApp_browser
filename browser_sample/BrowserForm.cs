using ParallelHybridApp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace browser_sample
{
    public partial class BrowserForm : Form
    {
        public enum EnumSearchState
        {
            None,
            Start,
            Processing,
            End
        }

        public EnumSearchState state { get; set; }
        public string keyword { get; set; }
        public List<SearchCompleteDataResult> search_result_ary { get; set; }

        public delegate void search_complete_delegate();
        public search_complete_delegate on_search_complete = null;

        public BrowserForm()
        {
            InitializeComponent();
        }

        public void start()
        {
            state = EnumSearchState.Start;
            search_result_ary = new List<SearchCompleteDataResult>();

            this.timer1.Start();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            switch (state)
            {
                case EnumSearchState.None:
                    break;
                case EnumSearchState.Start:

                    var url = "https://www.google.co.jp/search?q=" + keyword;
                    webBrowser1.Navigate(url);

                    state = EnumSearchState.Processing;

                    break;
                case EnumSearchState.Processing:

                    if( webBrowser1.ReadyState != System.Windows.Forms.WebBrowserReadyState.Complete)
                    {
                        return;
                    }

                    var doc = webBrowser1.Document.All.Cast<HtmlElement>();

                    var qry1 = doc.Where(elm => elm.Id == "bfoot");

                    if( qry1.Count() == 0)
                    {
                        return;
                    }

                    if (on_search_complete != null)
                    {
                        var qry2 = doc.Where(elm =>
                            elm.TagName == "DIV" && elm.GetAttribute("className") == "g"
                            );

                        if( qry2.Count() == 0)
                        {
                            return;
                        }

                        foreach(var elm2 in qry2)
                        {
                            HtmlElementCollection elm3 = elm2.GetElementsByTagName("A");

                            foreach(HtmlElement a_elm in elm3)
                            {
                                if( !a_elm.InnerHtml.Contains("LC20lb"))
                                {
                                    continue;
                                }

                                var title_elm = a_elm.GetElementsByTagName("H3");

                                var add_dat = new SearchCompleteDataResult()
                                {
                                    title = title_elm[0].InnerText,
                                    href = a_elm.GetAttribute("href")
                                };

                                search_result_ary.Add(add_dat);

                            }

                        }

                        state = EnumSearchState.End;

                        timer1.Stop();

                        on_search_complete();
                    }

                    break;
                case EnumSearchState.End:
                    break;
            }
        }
    }
}
