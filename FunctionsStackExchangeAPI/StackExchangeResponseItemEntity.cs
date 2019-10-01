using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionsStackExchangeAPI
{
    class StackExchangeResponseItemEntity : TableEntity
    {

        public StackExchangeResponseItemEntity(int creation_date, int question_id)
        {
            PartitionKey = Helpers.UnixTimeStampToDateTime(creation_date).Date.ToString("yyyyMMdd");
            RowKey = question_id.ToString();
        }
        public List<string> tags { get; set; }
        public StackExchangeResponseOwner owner { get; set; }
        public bool is_answered { get; set; }
        public int view_count { get; set; }
        public int answer_count { get; set; }
        public int score { get; set; }
        public int last_activity_date { get; set; }
        public int creation_date { get; set; }
        public int question_id { get; set; }
        public string link { get; set; }
        public string title { get; set; }
        public int? accepted_answer_id { get; set; }
    }


}
