using Segerfeldt.EventStore.Source;

using SourceConsoleApp;

using System;
using System.Data.SqlClient;

const string connectionString = "Server=localhost;Database=test;User Id=sa;Password=S_12345678;";
var connection = new SqlConnection(connectionString);
var store = new EventStore(connection);

var entityId = new EntityId("player3");
var increment = new ScoreIncrement {Points = 2};

var player = await store.ReconstituteAsync<Player>(entityId) ?? Player.RegisterNew(entityId, "Jones");
player.AwardPoints(3);

await store.PublishChangesAsync(player, "johan");
Console.WriteLine($"Player {player.Name} increased score by {increment.Points} points");
