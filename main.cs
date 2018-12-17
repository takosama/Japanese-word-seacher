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


        SQLiteConnection cn;
        public SQLiteUtil(string path)
        {
            cn = new SQLiteConnection("DataSource=" + path);
            cn.Open();
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
    }

    class Program
    {
        static void Main(string[] args)
        {
            SQLiteUtil util = new SQLiteUtil("wnjpn.db");
            while (true)
            {
                string str = Console.ReadLine();

                SQLiteUtil.SearchWordResult wordResult = util.SearchWord(str);
                Console.WriteLine("検索結果" + wordResult.GetResult.Count + "件ヒットしました");
                foreach (var s in wordResult.GetResult)
                {
                    Console.WriteLine(s);
                }
            }
        }
    }
}
