using Cache.Library.Configuration;
using Cache.Library.Management;

namespace Cache.Testes
{
    public class NoEvictionTest
    {
        private CacheManager CreateManager(long capacity = 1024 * 1024)
        {
            var options = new CacheOptions
            {
                Capacity = capacity,
                EvictionPolicy = "none"
            };
            return new CacheManager(options);
        }

        [Fact]
        public void Should_Add_And_Retrieve_Item_Successfully()
        {
            var cache = CreateManager();
            var data = new List<Dictionary<string, object>>
            {
                new() { { "id", 1 }, { "value", "teste" } }
            };

            var result = cache.AddItem("key1", "id", data, false, 1, out var message);

            Assert.True(result);
            Assert.Null(message);

            var item = cache.GetItem("key1", "1", out var getMessage);

            Assert.NotNull(item);
            Assert.Equal("teste", item["value"]);
            Assert.Null(getMessage);
        }

        [Fact]
        public void Should_Fail_To_Add_If_Over_Capacity()
        {
            var cache = CreateManager(0);

            var data = new List<Dictionary<string, object>>
            {
                new() { { "id", 1 }, { "value", "teste" } }
            };

            var result1 = cache.AddItem("key1", "id", data, false, 1, out var message1);
            Assert.False(result1);
            Assert.Equal("Os dados passados são maiores que a cache!", message1);
        }

        [Fact]
        public void Should_Set_Item_As_Stale()
        {
            var cache = CreateManager();
            var data = new List<Dictionary<string, object>>
            {
                new() { { "id", 1 }, { "value", "teste" } }
            };

            cache.AddItem("key1", "id", data, false, 1, out _);
            var result = cache.SetItemStale("key1");

            Assert.True(result);
        }

        [Fact]
        public void Should_Return_Null_When_Item_Expired()
        {
            var cache = CreateManager();
            var data = new List<Dictionary<string, object>>
            {
                new() { { "id", 1 }, { "value", "teste" } }
            };
            cache.AddItem("key1", "id", data, false, 0, out _);

            var item = cache.GetItem("key1", "1", out var message);

            Assert.Null(item);
            Assert.Equal("Este dado está inválido na cache!", message);
        }

        [Fact]
        public void Should_Return_Null_When_Item_Stale()
        {
            var cache = CreateManager();
            var data = new List<Dictionary<string, object>>
            {
                new() { { "id", 1 }, { "value", "teste" } }
            };

            cache.AddItem("key1", "id", data, false, 1, out _);
            cache.SetItemStale("key1");

            var item = cache.GetItem("key1", "1", out var message);

            Assert.Null(item);
            Assert.Equal("Este dado está inválido na cache!", message);
        }

        [Fact]
        public void Should_Work_Adjust_Values()
        {
            var cache = CreateManager();
            var data = new List<Dictionary<string, object>>
            {
                new() { { "id", 1 }, { "value", "teste" } }
            };

            cache.AddItem("key1", "id", data, false, 1, out _);
            cache.AdjustValues();
        }

        [Fact]
        public void Should_Return_Values()
        {
            var cache = CreateManager();
            var data = new List<Dictionary<string, object>>
            {
                new() { { "id", 1 }, { "value", "teste" } }
            };
            cache.AddItem("key1", "id", data, false, 1, out _);

            var result = cache.GetValues();

            Assert.Equal(result[0]["Key"], "key1");
        }
    }
}