//// Copyright © 2024 EPAM Systems

//using System.Diagnostics.Metrics;
//using Confluent.Kafka;

//namespace Epam.Kafka;

///// <summary>
///// 
///// </summary>
//public static class KafkaMetricsExtensions
//{
//    private static readonly Meter M = new Meter("Epam.Kafka.Consumer");
//    public static ConsumerBuilder<int, int> SetMetricsStatistics(this ConsumerBuilder<int, int> builder)
//    {
//        M.CreateObservableGauge("qwe", () => new Measurement<long>(0));

//        builder.SetStatisticsHandler((_, json) =>
//        {
//            Statistics s = Statistics.FromJson(json);


//        });


//        return builder;
//    }
//}