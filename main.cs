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
                         .Select(x => (_input[x]))
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

        public SearchWordResult SearchWordName(string wordid)
        {
            var cmd = cn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM word WHERE wordid={wordid}";
            var reader = cmd.ExecuteReader();

            string[] keys = { "wordid", "lemma", "pos" };
            var list = reader.GetValueList(keys);
            return new SearchWordResult(list);
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


    public class DataBaseManager
    {
        static SQLiteUtil util = new SQLiteUtil("wnjpn.db");
        public class Word
        {
            public string str;
            public string id;
            public string pos;

            public Word(string str, string id, string pos)
            {
                Init(str, id, pos);
            }

            private void Init(string str, string id, string pos)
            {
                this.str = str;
                this.id = id;
                this.pos = pos;
            }
            public Word(string wordid)
            {
                var tmp = util
                    .SearchWordName(wordid)
                    .GetResult
                    .First();
                Init(tmp.lemma, tmp.wordid, tmp.pos);
            }

            public static IEnumerable<Word> GetWords(string word) =>
                util
                .SearchWord(word)
                .GetResult
                .Select(x => new Word(x.lemma, x.wordid, x.pos));

            public void View() =>
                Console.WriteLine($"単語名={str} ID={id} Pos={pos}");
            public string Name() => str;

            public IEnumerable<Syn> GetSyns() =>
             util.SearchSenseSynSet(id)
                     .SynSetArray
                     .Select(x => new Syn(x));
        }

        public class Syn
        {
            public string SynStr;
            public List<Word> wordList = new List<Word>();
            public Syn(string synstr)
            {
                this.SynStr = synstr;
                this.wordList = util
                     .SearchSenseWord(synstr)
                     .WordidArray
                     .Select(x => new Word(x))
                     .ToList();
            }

            public static IEnumerable<Syn> GetSyns(Word word) =>
                util.SearchSenseSynSet(word.id)
                    .SynSetArray
                    .Select(x => new Syn(x));

            public void View()
            {
                Console.WriteLine($"syn={SynStr}");
                var wordStr = string.Join(" ", wordList.Select(x => x.Name()));
                Console.WriteLine(wordStr);
            }

            public IEnumerable<string> GetWordNames =>
                wordList.Select(x => x.Name());
        }
        public class Result
        {
            IEnumerable<Word> words;
            IEnumerable<IEnumerable<Syn>> synSets;
            public Result(string wordstr)
            {
                words = Word.GetWords(wordstr);
                synSets = words.Select(x => x.GetSyns());
            }
            public IEnumerable<(Word word, IEnumerable<Syn> synSet)> GetResult() =>
                words.Zip(synSets, (word, synSet) => (word, synSet));
            public IEnumerable<string> GetWordNames()
            {
                var tmp = synSets.Select(x => x.Select(y => y.GetWordNames));
                List<string> list = new List<string>();
                foreach (var n in tmp)
                    foreach (var m in n)
                        foreach (var o in m)
                            list.Add(o);
                return list.Distinct();
            }
        }



        public void Search(string str)
        {
            var result = new Result(str);

            Console.WriteLine(string.Join(" ", result.GetWordNames()));
            Console.WriteLine();
            Console.WriteLine();
            foreach (var res in result.GetResult())
            {
                res.word.View();
                foreach (var syn in res.synSet)
                {
                    Console.WriteLine();
                    syn.View();
                }
            }
        }
    }




    class Program
    {
        static void Main(string[] args)
        {
            DataBaseManager manager = new DataBaseManager();
            while (true)
            {
                string str = Console.ReadLine();
                Console.Clear();
                Console.WriteLine("検索文字");
                Console.WriteLine(str);
                manager.Search(str);
            }
        }
    }
}
