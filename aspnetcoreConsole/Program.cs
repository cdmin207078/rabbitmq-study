using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;

namespace aspnetcoreConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // simple queue & work queue - 简单队列 / 工作队列
            // SimpleQueueConsumerListener();

            // Publish / Subscribe - 发布订阅
            // ExchangesConsumerListener();

            // Routing - 路由
            // ExchangesRoutingConsumerListener();

            // Topics - 主题
            //ExchangesTopicsConsumerListener();

            // Transaction - 简单队列接受消息
            //TransactionQueueConsumerListener();

            // Confirm - 简单队列接受 confirm 模式消息
            ConfirmQueueConsumerListener();
        }

        private static IConnection CreateRabbitConnection()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "192.168.0.109",
                Port = 5672,
                VirtualHost = "/vhost_mmr",
                UserName = "user_mmr",
                Password = "123456"
            };

            return factory.CreateConnection();
        }

        private static void SimpleQueueConsumerListener()
        {
            Console.WriteLine("模式任务执行时长, 单位：ms");
            var delaySeconds = Convert.ToInt32(Console.ReadLine());
            var No = 1;

            using (var connection = CreateRabbitConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "halo",
                                         //durable: false,
                                         durable: true, // 设置 队列可持久化
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    // 告诉 RabbitMQ 同一时间, 一次只读取一条消息, 等处理完毕, 再获取下一条
                    // * 此时需要配合关闭自动回复 { autoAck : false }
                    channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body);

                        Console.WriteLine($"======= 开始处理 {message} 号任务 ========");

                        // 模拟任务执行时长
                        Thread.Sleep(delaySeconds);

                        // 主动返回
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                        Console.WriteLine($"======= 处理完毕 {message} 号任务 ========");

                        Console.WriteLine("[{0}] Recevied: {1}", No, message);

                        No++;
                    };

                    channel.BasicConsume(queue: "halo",
                                         //autoAck: true,
                                         autoAck: false, // 关闭 "自动确认模式", 待消费者确定执行完毕之后, 再主动返回确认消息
                                         consumer: consumer);

                    Console.WriteLine(" Programs is running, Press [enter] to exit.");
                    Console.ReadLine();
                }
            }
        }

        private static void ExchangesConsumerListener()
        {
            Console.WriteLine("请输入消费者名称: ");
            var consumerName = Console.ReadLine();

            using (var connection = CreateRabbitConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    var exchangeName = "advertising";
                    channel.ExchangeDeclare(exchange: exchangeName, type: "fanout");

                    var queueName = channel.QueueDeclare().QueueName;
                    channel.QueueBind(queueName, exchangeName, routingKey: "");

                    Console.WriteLine($" [{consumerName}] Waiting....");

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body);

                        Console.WriteLine($" [{consumerName}] Recevied: {message}");
                    };

                    channel.BasicConsume(queue: queueName,
                                         autoAck: true,
                                         consumer: consumer);

                    Console.WriteLine(" Press [enter] to exit.");
                    Console.ReadLine();
                }
            }
        }

        private static void ExchangesRoutingConsumerListener()
        {
            Console.WriteLine("请输入消费者名称: ");
            var consumerName = Console.ReadLine();

            Console.WriteLine("请输入可接收的日志类型(多个用 , 分隔): ");
            var routingKeys = Console.ReadLine();

            using (var connection = CreateRabbitConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    var exchangeName = "recording-logs";
                    channel.ExchangeDeclare(exchange: exchangeName, type: "direct");

                    var queueName = channel.QueueDeclare().QueueName;

                    if (string.IsNullOrWhiteSpace(routingKeys))
                        channel.QueueBind(queueName, exchangeName, routingKey: "");
                    else
                    {
                        foreach (var routingKey in routingKeys.Split(","))
                        {
                            channel.QueueBind(queueName, exchangeName, routingKey);
                        }
                    }

                    Console.WriteLine($" [{consumerName}] Waiting....");

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body);

                        Console.WriteLine($" [{consumerName}] Recevied: {message}");
                    };

                    channel.BasicConsume(queue: queueName,
                                         autoAck: true,
                                         consumer: consumer);

                    Console.WriteLine(" Press [enter] to exit.");
                    Console.ReadLine();
                }
            }
        }

        private static void ExchangesTopicsConsumerListener()
        {
            Console.WriteLine("请输入消费者名称: ");
            var consumerName = Console.ReadLine();

            Console.WriteLine("请输入可接收的主题模式(多个用 , 分隔): ");
            var routingKeys = Console.ReadLine();

            using (var connection = CreateRabbitConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    var exchangeName = "recording-logs-topic";
                    channel.ExchangeDeclare(exchange: exchangeName, type: "topic");

                    var queueName = channel.QueueDeclare().QueueName;

                    if (string.IsNullOrWhiteSpace(routingKeys))
                        channel.QueueBind(queueName, exchangeName, routingKey: "");
                    else
                    {
                        foreach (var routingKey in routingKeys.Split(","))
                        {
                            channel.QueueBind(queueName, exchangeName, routingKey);
                        }
                    }

                    Console.WriteLine($" [{consumerName}] Waiting....");

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body);

                        Console.WriteLine($" [{consumerName}] Recevied: {message}");
                    };

                    channel.BasicConsume(queue: queueName,
                                         autoAck: true,
                                         consumer: consumer);

                    Console.WriteLine(" Press [enter] to exit.");
                    Console.ReadLine();
                }
            }
        }

        private static void TransactionQueueConsumerListener()
        {
            Console.WriteLine("请输入消费者名称: ");
            var consumerName = Console.ReadLine();

            using (var connection = CreateRabbitConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare("simple_queue_tx", false, false, false, null);

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body);

                        Console.WriteLine("[{0}] Recevied: {1}", consumerName, message);
                    };

                    channel.BasicConsume(queue: "simple_queue_tx",
                                         autoAck: true,
                                         consumer: consumer);

                    Console.WriteLine(" Programs is running, Press [enter] to exit.");
                    Console.ReadLine();
                }
            }
        }

        private static void ConfirmQueueConsumerListener()
        {
            Console.WriteLine("请输入消费者名称: ");
            var consumerName = Console.ReadLine();

            using (var connection = CreateRabbitConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare("simple_queue_confirm", false, false, false, null);

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body);

                        Console.WriteLine("[{0}] Recevied: {1}", consumerName, message);
                    };

                    channel.BasicConsume(queue: "simple_queue_confirm",
                                         autoAck: true,
                                         consumer: consumer);

                    Console.WriteLine(" Programs is running, Press [enter] to exit.");
                    Console.ReadLine();
                }
            }
        }
    }
}
