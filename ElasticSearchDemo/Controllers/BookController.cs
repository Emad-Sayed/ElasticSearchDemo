using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticSearchDemo.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class BookController : ControllerBase
    {
        private readonly ElasticClient _client;
        public BookController(ElasticClient client)
        {
            _client = client;
        }
        [HttpPost]
        public ActionResult CreateIndex()
        {
            var settings = new IndexSettings { NumberOfReplicas = 1, NumberOfShards = 2 };

            var indexConfig = new IndexState
            {
                Settings = settings
            };
            var response = _client.Indices.Create("books",
                                index =>
                                index.InitializeUsing(indexConfig)
                                .Map<Book>(
                                    x => x.AutoMap()
                                ));
            return Ok();
        }
        [HttpPost]
        public ActionResult<bool> AddBook([FromBody] Book book)
        {
            var bookDetails = new Book()
            {
                Authors = "s",
                Categories = "1",
                Isbn = "false",
                LongDescription = "asdasd asd",
                PageCount = 1,
                ShortDescription = "2",
                Status = "done",
                ThumbnailUrl = "weq.com"
            };
            var result = _client.IndexDocument(bookDetails); //Will Save in the default Index
            return result.IsValid;
        }
        [HttpPost]
        public ActionResult<bool> AddBulkBooks([FromBody] Book book)
        {
            var listOfBooks = new List<Book>();
            for (int i = 0; i < 1000; i++)
            {
                listOfBooks.Add(new Book
                {
                    Authors = i + "s",
                    Categories = i + "1",
                    Isbn = "false",
                    LongDescription = i + "asdasd asd",
                    PageCount = i,
                    ShortDescription = i + "2",
                    Status = i + "done",
                    ThumbnailUrl = i + "weq.com"
                });
            }
            var bulkIndexer = new BulkDescriptor();

            foreach (var element in listOfBooks)
                bulkIndexer.Index<Book>(i => i
                  .Document(element));

            var result = _client.Bulk(bulkIndexer);
            return result.IsValid;
        }
        [HttpGet]
        public ActionResult<Book> GetBook()
        {
            var results1 = _client.Search<Book>(s => s
            .Preference("_shards:1") // Specific Shard
            .Query(q => q.MatchAll()));

            //Exact Search
            var results2 = _client.Search<Book>(s => s
            //.Preference("_shards:0") // Specific Shard
            .Query(q => q
            .Term(t => t
            .Field(f => f.PageCount)
            .Value("3"))));

            //any part Search
            var results3 = _client.Search<Book>(s => s
            .Query(q => q
            .Match(t => t
            .Field(f => f.ThumbnailUrl)
            .Query("weq.co"))));
            return Ok(results3.Documents.ToList());
        }
        [HttpGet]
        public ActionResult<Book> GetAggregateBook()
        {
            var results = _client.Search<Book>(s => s
         .Query(q => q
             .MatchAll()
         )
         .Aggregations(a => a
             .Range("pageCounts", r => r
                 .Field(f => f.PageCount)
                 .Ranges(r => r.From(0),
                         r => r.From(200).To(400),
                         r => r.From(400).To(600),
                         r => r.From(600)
                 )
             )
         )
     );
            return Ok(results.Documents.ToList());
        }
    }
}
