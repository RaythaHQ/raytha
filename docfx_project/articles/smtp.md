# Configure Raytha to send emails

## Configuring SMTP server for Raytha

If you are running Raytha on your local development environment, you can use a tool such as [Papercut SMTP](https://github.com/ChangemakerStudios/Papercut-SMTP). Papercut SMTP is a desktop client that can accept emails at the default SMTP settings as shown in your `/src/Raytha.Web/appsettings.json` file.

You can configure your appsettings.json file by providing the following information:

```
  "SMTP_HOST": "127.0.0.1",
  "SMTP_PORT": 25,
  "SMTP_USERNAME": "",
  "SMTP_PASSWORD": "",
```

* SMTP_HOST: The IP address or hostname of the SMTP server you wish to use. In the case of local development with Papercut SMTP, use "127.0.0.1".
* SMTP_PORT: The port number of the SMTP server. In the case of local development with Papercut SMTP, use the default port number 25.
* SMTP_USERNAME and SMTP_PASSWORD: The credentials required to authenticate with the SMTP server. Leave these fields blank if your SMTP server does not require authentication.

When deploying Raytha to production, it is recommended to use a proper transactional email service. Some popular transactional email services are [Sendgrid](https://sendgrid.com) and [Mailgun](https://www.mailgun.com/). These services offer greater reliability and scalability, as well as more advanced features such as email tracking and analytics.

## Using Sendgrid

Sendgrid credentials will commonly appear similar to the below:

```
"SMTP_HOST": "smtp.sendgrid.net",
"SMTP_PORT": 587,
"SMTP_USERNAME": "apikey",
"SMTP_PASSWORD": "SG.xxxxxxx.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
```

And `SMTP_PASSWORD` will be your Sendgrid api key.

