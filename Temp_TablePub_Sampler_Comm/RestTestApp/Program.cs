using RestTestApp;
using StateOfTheArtTablePublisher;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

var a = new A();
var tt = new TT();
Task.Run(() => tt.PubSub_N_Records());
Task.Run(() => tt.PubSub_N_Records_UpdateAsInsertForLowLatency());

app.Run();
