using System;
using Microsoft.Extensions.Configuration;

namespace TestCompile
{
    class Program
    {
        static void Main(string[] args)
        {
            // 测试 ConfigurationBuilder 是否能编译
            var config = new ConfigurationBuilder()
                .AddJsonFile("test.json", optional: true)
                .Build();
                
            Console.WriteLine("ConfigurationBuilder 编译测试通过！");
        }
    }
}