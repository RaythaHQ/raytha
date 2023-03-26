# Deploy Raytha to Production

Deploying Raytha CMS to production is similar to deploying any other .NET 6+ web application. However, there are some important considerations to keep in mind before deploying:

* Choose the deployment environment that best fits your needs, such as a bare metal VM, a PaaS solution like Azure App Service, or a containerized solution like Docker.
* Decide on a file storage provider, such as Azure Blob Storage or an S3-compatible storage, that best suits your requirements.
* Choose between self-hosting your SQL Server database or using a managed provider like Azure SQL.

Our team has extensively tested Raytha on various deployment environments and storage providers. For instance, we have successfully deployed Raytha on Azure App Service's Windows environment as well as Web Apps for Containers. We have also tested Raytha's file storage in both `Local` mode and with Azure Blob Storage. While using Azure Blob Storage is recommended for production, Local mode is sufficient for testing and development purposes.

Furthermore, we have deployed Raytha's database on Azure SQL and tested it in Azure Kubernetes Service. We found that this setup was not only effective but also easy to configure.

## Installation Support

If you are not comfortable with setting up hosting for Raytha, don't worry! Our team at Raytha is here to help. We offer guided installation services to help you get up and running quickly. Additionally, we also offer managed hosting services so that you don't have to worry about the technical details of hosting Raytha yourself. Please reach out to us at hello@raytha.com to learn more about our services and how we can help you.

## Run at a different path base

Raytha allows you to specify a custom path base route by setting the optional `PATHBASE` environment variable. This is particularly useful if you have multiple web applications running on a single domain, being routed to via proxy like nginx, or multiple Raytha websites running on the same domain.

For instance, let's say you are running an annual conference for your association and you want to keep the content of previous years' conferences intact while dedicating a separate website for the current year's conference. You might want a configuration that looks like this:

* annualconference.mydomain.com/2023
* annualconference.mydomain.com/2024
* annualconference.mydomain.com/2025

Each route would represent a separate instance of Raytha. You can achieve this by setting the `PATHBASE` environment variable to `/2023`, `/2024`, and `/2025` respectively.

## Run Raytha behind a proxy

A popular environment setup for Raytha is to host it on a provider like Azure, with traffic secured behind a web application firewall and reverse proxied through a service like Cloudflare. While this configuration provides reasonable confidence in the security of your traffic, you may encounter http and https issues on the hosting provider side. It's worth noting that this is not unique to Raytha, but rather a common challenge for all .NET developers who wish to run their platforms. To learn more, please refer to the resources listed below.

* [Solving issues with https and SSL with Azure App Services behind a Cloudflare reverse proxy](https://raytha.com/blog/NET-application-on-Azure-App-Services-https-issues-behind-Cloudflare)
* [Debugging http vs https with your .NET app running on AKS](https://raytha.com/blog/Debugging-http-vs-https-issues-on-your-NET-app-deployed-on-AKS)
