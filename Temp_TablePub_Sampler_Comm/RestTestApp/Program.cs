using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RestTestApp;
using RP.Prober.CyclicCacheProbing;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

//#if !NET6_0
    builder.Services.AddSwaggerGen();
//#endif

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
//#if !NET6_0
    app.UseSwagger();
    app.UseSwaggerUI();
//#endif
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


var measurmentsCreator = new MeasurmentsCreator("Demo1", HeaderType.Row, 1);
measurmentsCreator.StartGenerateLines();

var measurmentsCreator2 = new MeasurmentsCreator("Demo2", HeaderType.Column, 7);
measurmentsCreator2.StartGenerateLines();

var samplesCreator = new SamplesCreator("Demo3");
samplesCreator.StartGenerateLines();



var mcc = new MeasurmentsCountersCreator("Ran/DotNetCounters", new List<string>() { "Cpu", "Memory", "Gc", "Pcu" });
mcc.StartGenerateLines();
var mcc2 = new MeasurmentsCountersCreator("Ran2/DotNetCounters", new List<string>() { "Memory", "Cpu", "Pcu", "Gc" });
mcc2.StartGenerateLines();
var mcc3 = new MeasurmentsCountersCreator("Ran3/DotNetCounters", new List<string>() { "Memory", "Cpu", "Pcu", "Gc" });
mcc3.StartGenerateLines();
var mcc4 = new MeasurmentsCountersCreator("Ran4/DotNetCounters", new List<string>() { "Memory", "Cpu", "Pcu", "Gc" });
mcc4.StartGenerateLines();
var mcc5 = new MeasurmentsCountersCreator("Ran5/DotNetCounters", new List<string>() { "Memory", "Cpu", "Pcu", "Gc" });
mcc5.StartGenerateLines();


var tt = new TT();
Task.Run(() => tt.PubSub_N_Records());
Task.Run(() => tt.PubSub_N_Records_UpdateAsInsertForLowLatency());

app.Run();
