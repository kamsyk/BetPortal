# Use official Node.js image for building the Angular app
FROM node:20 AS build

WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
#RUN npm run build -- --output-path=dist
RUN npm run build -- --configuration production --output-path=dist

# Use official Nginx image to serve the built app
FROM nginx:alpine

# Remove default Nginx static assets
RUN rm -rf /usr/share/nginx/html/*

# Copy built Angular app from previous stage
COPY --from=build /app/dist/browser /usr/share/nginx/html
#COPY --from=build /app/dist/bet-portal-admin /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
