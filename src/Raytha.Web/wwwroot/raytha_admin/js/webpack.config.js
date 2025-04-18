const path = require("path");

module.exports = (env, argv) => {
   const isDevBuild = !(argv.mode && argv.mode == "production");

   let config = {
      entry: "./src/application.js",
      output:
      {
         filename: "main.js",
         path: path.resolve(__dirname, "dist"),
      },
      resolve: {
         extensions: ['.ts', '.js'],
         alias: {
            'wysiwyg': path.resolve(__dirname, 'src/wysiwyg'),
            'uppyWrapper': path.resolve(__dirname, 'src/uppyWrapper'),
         }
      },
      module:
      {
         rules: [
            {
               test: /\.(ts)$/,
               exclude: /node_modules/,
               use: [
                  {
                     loader: "babel-loader",
                     options: {
                        presets: [
                           "@babel/preset-env",
                           "@babel/preset-typescript"
                        ],
                        plugins: [
                           "@babel/plugin-transform-typescript"
                        ]
                     }
                  }
               ]
            },
            {
               test: /\.m?js$/,
               exclude: /node_modules/,
               use: {
                  loader: "babel-loader",
                  options: {
                     presets: [
                        ["@babel/preset-env", { targets: "defaults" }]
                     ]
                  }
               }
            },
            {
               test: /\.css$/,
               use: ['style-loader', 'css-loader']
            },
            {
               test: /\.ttf$/,
               use: ['file-loader']
            },
            {
               test: /\.html$/,
               loader: 'html-loader',
               options: {
                  minimize: true,
               }
            },
         ],
      },
   };

   if (isDevBuild);
   config.devtool = "source-map";

   return config;
};