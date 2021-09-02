using Segerfeldt.EventStore.Source;

using SourceConsoleApp;

using System;
using System.Data.SqlClient;

const string connectionString = "Server=localhost;Database=test;User Id=sa;Password=S_12345678;";
var connectionPool = new SqlConnectionPool(connectionString);
var publisher = new EventPublisher(connectionPool);
var store = new EntityStore(connectionPool);

var entityId = new EntityId("player3");
var player = await store.ReconstituteAsync<Player>(entityId, Player.EntityType) ?? Player.RegisterNew(entityId, "Jones");

const int points = 2;
player.AwardPoints(points);

await publisher.PublishChangesAsync(player, "johan");
Console.WriteLine($"Player {player.Name} increased score by {points} points");
