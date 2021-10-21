using Fundamentos.RabbitMQ.Consumers;
using Fundamentos.RabbitMQ.Options;
using Fundamentos.RabbitMQ.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MassTransit;

namespace Fundamentos.RabbitMQ
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<RabbitMqConfiguration>(Configuration.GetSection("RabbitMqConfig"));
            services.AddScoped<INotificationService, NotificationService>();

            #region Consumidores Manuais
            services.AddHostedService<MessagesConsumer>();

            services.AddHostedService<DirectExchangeProdutosHardwareConsumer>();
            services.AddHostedService<DirectExchangeProdutosSoftwareConsumer>();

            services.AddHostedService<FanoutExchangeDashboardConsumer>();
            services.AddHostedService<FanoutExchangeEstoqueConsumer>();
            services.AddHostedService<FanoutExchangePagamentosConsumer>();

            services.AddHostedService<TopicExchangeVitoriaConsumer>();
            services.AddHostedService<TopicExchangeRioConsumer>();
            #endregion

            //#region Consumidores MassTransit
            //services.AddMassTransit(bus =>
            //{
            //    bus.UsingRabbitMq((ctx, busConfigurator) =>
            //    {
            //        busConfigurator.Host(Configuration.GetConnectionString("RabbitMq"));
            //    });
            //});
            //services.AddMassTransitHostedService();
            //#endregion

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Fundamentos.RabbitMQ", Version = "v1" });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fundamentos.RabbitMQ v1"));
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
