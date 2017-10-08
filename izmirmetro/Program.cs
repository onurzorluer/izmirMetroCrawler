using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net;
using LiteDB;

namespace izmirmetro
{
    class Program
    {
        static void Main(string[] args)
        {



            string url = "http://www.izmirmetro.com.tr/SeferPlani/35";
            string htmlContent = GetContent(url);

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlContent);

            var GunNodes = doc.DocumentNode.SelectNodes(@"//th[@class=""subpage-sefersaatleri-tarife""]");

            var saatNodes = doc.DocumentNode.SelectNodes(@"//div[@class=""subpage-sefersaatleri-tablecontainer fl""]");

            if (saatNodes != null)
            {

                for (int i = 0; i < GunNodes.Count; i++)  //GunNodes.Count
                {

                    Sefer nSefer = new Sefer();

                    nSefer.SaatAraligii = new List<string>();
                    nSefer.SeferSikligii = new List<string>();

                    Metro nMetro = new Metro();
                    nMetro.gunu = new List<Gn>();


                    string gunn = GunNodes[i].InnerText;

                    nSefer.Gun = gunn;

                    string nodeshtml = saatNodes[i].InnerHtml;

                    var htmlDocnode = new HtmlDocument();
                    htmlDocnode.LoadHtml(nodeshtml);
                    var partsaatNodes = htmlDocnode.DocumentNode.SelectNodes(@"//tr/td");
                    int s = 0;
                    foreach (var partsaatnode in partsaatNodes)
                    {

                        Console.WriteLine(partsaatnode.InnerText);
                        if (s % 2 == 0)
                        {
                            nSefer.SaatAraligii.Add(partsaatnode.InnerText);
                        }
                        else
                        {
                            nSefer.SeferSikligii.Add(partsaatnode.InnerText);
                        }

                        s++;

                    }
                    for (int j = 0; j < nSefer.SaatAraligii.Count; j++)
                    {
                        Gn nGn = new Gn();
                        string value = nSefer.SaatAraligii[j];

                        char[] delimiter = new char[] { '-' };
                        string[] parts = value.Split(delimiter,
                                         StringSplitOptions.RemoveEmptyEntries);

                        nGn.BaslangicSaat = parts[0];
                        nGn.BitisSaat = parts[1];
                        nGn.SeferSikligi = nSefer.SeferSikligii[j];
                        nGn.Gun = gunn;

                        nMetro.gunu.Add(nGn);
                        nMetro.GunNo = i;
                    }

                    add2mdb(nMetro);

                }

                Console.ReadLine();
            }



        }

        private static string GetContent(string urlAddress)
        {
            Uri url = new Uri(urlAddress);
            WebClient client = new WebClient();
            client.Encoding = System.Text.Encoding.UTF8;
            string html = client.DownloadString(url);

            return html;
        }

        private static void add2mdb(Metro md)
        {
            if (md == null)
            {
                Console.WriteLine("NULL");
                return;

            }
            using (var db = new LiteDatabase(@"botart_ulasim_metro.adbx"))
            {
                // Get hat collection
                var dbmtr = db.GetCollection<Metro>("metro");
                var results = dbmtr.Find(x => x.GunNo == md.GunNo).FirstOrDefault();
                if (results == null)
                {
                    md._id = Guid.NewGuid().ToString();
                    dbmtr.Insert(md);

                }
                else
                {
                    //Burada hattın bilgileri guncellenebilir
                    Console.WriteLine("UPDATING " + results._id);
                    dbmtr.Update(results._id, md);

                }

                dbmtr.EnsureIndex(x => x.GunNo);
            }
        }
    }





    public class Metro
    {
        public string _id { get; set; }
        public int GunNo { get; set; }
        public List<Gn> gunu { get; set; }
    }

    public class Gn
    {
        public string Gun { get; set; }
        public string BaslangicSaat { get; set; }
        public string BitisSaat { get; set; }
        public string SeferSikligi { get; set; }
    }

    public class Sefer
    {
        public List<string> SaatAraligii { get; set; }
        public List<string> SeferSikligii { get; set; }
        public string Gun { get; set; }
    }
}
