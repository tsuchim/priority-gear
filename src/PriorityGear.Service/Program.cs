using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PriorityGear.Service;
using PriorityGear.Windows;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "PriorityGear Service");
builder.Services.AddSingleton<PrivilegeService>();
builder.Services.AddSingleton<Win32PriorityApplier>();
builder.Services.AddSingleton<MachineRuleStore>();
builder.Services.AddSingleton<ServiceFileLog>();
builder.Services.AddHostedService<PriorityGearWorker>();

await builder.Build().RunAsync();
