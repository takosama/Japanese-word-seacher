using System;
using System.Data.SQLite;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Xml;
using System.Configuration;
using System.Data;
using System.Text;

namespace 類義語とか調べるやつ
{
    public static class SQLiteExtension
    {
        public static List<List<string>> GetValueList(this SQLiteDataReader reader, string[] keyArray)
        {
            var indexs = Enumerable.Range(0, keyArray.Length)
                         .Select(i =>
                      Enumerable.Range(0, reader.GetValues().Keys.Count)
                         .Select(x => (x, reader.GetValues().GetKey(x)))
                         .First(x => x.Item2 == keyArray[i]).x)
                         .ToArray();

            List<List<string>> rtn = new List<List<string>>();
            while (reader.Read())
            {
                rtn.Add(Enumerable.Range(0, indexs.Length).Select(x => reader.GetValue(indexs[x]).ToString()).ToList());
            }
            return rtn;
        }

        public static string DumpQuery(this SQLiteDataReader reader)
        {
            var i = 0;
            var sb = new StringBuilder();
            while (reader.Read())
            {
                if (i == 0)
                {
                    sb.AppendLine(string.Join("\t", reader.GetValues().AllKeys));
                    sb.AppendLine(new string('=', 8 * reader.FieldCount));
                }
                sb.AppendLine(string.Join("\t", Enumerable.Range(0, reader.FieldCount).Select(x => reader.GetValue(x))));
                i++;
            }
            return sb.ToString();
        }
    }


    class SQLiteUtil
    {
        public class SearchWordResult
        {
            List<List<string>> _input;
            public SearchWordResult(List<List<string>> input)
            {
                this._input = input;
            }

            public List<(string wordid, string lemma, string pos)> GetResult =>
                         Enumerable.Range(0, _input.Count)
                         .Select(x => (_input[x][0], _input[x][1], _input[x][2]))
                         .ToList();
        }
        public class SearchWordNamReseult
        {
            List<string> _input;
            public SearchWordNamReseult(List<string> input)
            {
                this._input = input;
            }

            public List<string> WordidArray =>
                         Enumerable.Range(0, _input.Count)
                         .Select(x => (_input[x]))
                         .ToList();
        }
        public class SearchSenceWordidResult
        {
            List<string> _input;
            public SearchSenceWordidResult(List<string> input)
            {
                this._input = input;
            }

            public List<string> WordidArray =>
                         Enumerable.Range(0, _input.Count)
                         .Select(x => (_input[x] ))
                         .ToList();
        }
        public class SearchSenceSynSetResult
        {
            List<string> _input;
            public SearchSenceSynSetResult(List<string> input)
            {
                this._input = input;
            }

            public List<string> SynSetArray =>
                         Enumerable.Range(0, _input.Count)
                         .Select(x => (_input[x]))
                         .ToList();
        }


        SQLiteConnection cn;
        public SQLiteUtil(string path)
        {
            cn = new SQLiteConnection("DataSource=" + path);
            cn.Open();
        }

        public SearchWordNamReseult SearchWordName(string wordid)
        {
            var cmd = cn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM word WHERE wordid={wordid}";
            var reader = cmd.ExecuteReader();

            string[] keys = { "lemma" };
            var list = reader.GetValueList(keys).Select(x=>x[0]).ToList();
            return new SearchWordNamReseult(list);
        }

        public SearchWordResult SearchWord(string str)
        {
            var cmd = cn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM word WHERE lemma='{str}'";
            var reader = cmd.ExecuteReader();

            string[] keys = { "wordid", "lemma", "pos" };
            var list = reader.GetValueList(keys);
            return new SearchWordResult(list);
        }

        public SearchSenceSynSetResult SearchSenseSynSet(string wordid)
        {
            var cmd = cn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM sense WHERE wordid={wordid}";
            var reader = cmd.ExecuteReader();

            string[] keys = { "synset" };
            var list = reader.GetValueList(keys).Select(x => x[0]).ToList();
            return new SearchSenceSynSetResult(list);
        }

        public SearchSenceWordidResult SearchSenseWord(string synset)
        {
            var cmd = cn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM sense WHERE synset='{synset}'";
            var reader = cmd.ExecuteReader();

            string[] keys = { "wordid" };
            var list = reader.GetValueList(keys).Select(x => x[0]).ToList();
            return new SearchSenceWordidResult(list);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            SQLiteUtil util = new SQLiteUtil("wnjpn.db");
            while (true)
            {
                string str = Console.ReadLine();
                Console.Clear();
                Console.WriteLine("検索文字");
                Console.WriteLine(str);
                SQLiteUtil.SearchWordResult wordResult = util.SearchWord(str);
              
                var synset = wordResult.GetResult
                    .Select(x => x.wordid)
                    .Select(x => util.SearchSenseSynSet(x))
                    .Select(x => x.SynSetArray)
                    .ToArray();

                var synwordidSet = synset
                    .Select(x => (
                    sysnset: x,
                    wordidset: x.Select(y => util.SearchSenseWord(y)).ToArray()))
                    .ToArray();

                var bviewData = wordResult.GetResult
                    .Zip(
                    synwordidSet,
                    (x, y) => (word: x, synset: y.sysnset.Zip(y.wordidset, (z, w) => (synset: z, wordidset: w))))
                    .ToArray();

                var viewData = bviewData
                    .Select(x => (word: x.word, reslt: x.synset.Select(y => (y.synset, wordNameset: y.wordidset.WordidArray.Select(w => util.SearchWordName(w).WordidArray[0])))));

                Console.WriteLine();

                Console.Write("ヒットした結果");
                var words = viewData.Select(x => x.reslt.Select(y => y.wordNameset.ToArray()).ToArray()).ToArray();

                List<string> wordlist = new List<string>();
                foreach (var n in words)
                    foreach (var m in n)
                        foreach (var o in m)
                            wordlist.Add(o);
                wordlist = wordlist.Distinct().ToList();

                Console.WriteLine($" {wordlist.Count}件");
                
                Console.WriteLine(string.Join("\t", wordlist));

                Console.WriteLine();

                foreach (var n in viewData)
                {
                    Console.WriteLine(n.word);
                    Console.WriteLine("類義概念単語群");
                    foreach (var m in n.reslt)
                    {
                        Console.WriteLine("概念名");
                        Console.WriteLine(m.synset);
                        Console.WriteLine("所属単語");
                        Console.WriteLine(string.Join(" ",m.wordNameset));
                    }
                }
            }
        }
    }
}
