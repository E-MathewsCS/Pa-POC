FROM node:20 AS build
WORKDIR /web
COPY . .
RUN npm install --no-audit --no-fund && npm run build

FROM nginx:1.27-alpine
COPY --from=build /web/dist /usr/share/nginx/html
RUN printf "\
server {\n\
  listen 80;\n\
  root /usr/share/nginx/html;\n\
  index index.html;\n\
  location / { try_files \$uri \$uri/ /index.html; }\n\
  location /parcels { proxy_pass http://api:8080; }\n\
  location /parcel  { proxy_pass http://api:8080; }\n\
}\n" > /etc/nginx/conf.d/default.conf
EXPOSE 80
