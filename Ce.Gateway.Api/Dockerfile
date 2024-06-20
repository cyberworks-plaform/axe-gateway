FROM alpine:3.9.4

# Add some libs required by .NET runtime 
# https://github.com/dotnet/core/blob/master/Documentation/build-and-install-rhel6-prerequisites.md#troubleshooting
RUN apk add --no-cache \ 
    openssh libunwind \
    nghttp2-libs libidn krb5-libs libuuid lttng-ust zlib \
    libstdc++ libintl \
    icu

EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS http://*:80
ENV ServiceCode Gateway
ENV Tokens_Key 0123456789ABCDEF
ENV Tokens_Issuer http://ce.com.vn
ENV Tokens_Audience http://ce-axe.com.vn
ENV GlobalConfiguration__BaseUrl http://gateway
ENV Routes__0__DownstreamScheme http
ENV Routes__0__DownstreamHostAndPorts__0__Host auth
ENV Routes__0__DownstreamHostAndPorts__0__Port 80
ENV Routes__1__DownstreamScheme http
ENV Routes__1__DownstreamHostAndPorts__0__Host file
ENV Routes__1__DownstreamHostAndPorts__0__Port 80
ENV Routes__2__DownstreamScheme http
ENV Routes__2__DownstreamHostAndPorts__0__Host ocr
ENV Routes__2__DownstreamHostAndPorts__0__Port 80
ENV Routes__3__DownstreamScheme http
ENV Routes__3__DownstreamHostAndPorts__0__Host commonmasterdata
ENV Routes__3__DownstreamHostAndPorts__0__Port 80
ENV Routes__4__DownstreamScheme http
ENV Routes__4__DownstreamHostAndPorts__0__Host log
ENV Routes__4__DownstreamHostAndPorts__0__Port 80
ENV Routes__5__DownstreamScheme http
ENV Routes__5__DownstreamHostAndPorts__0__Host notification
ENV Routes__5__DownstreamHostAndPorts__0__Port 80
ENV Routes__6__DownstreamScheme http
ENV Routes__6__DownstreamHostAndPorts__0__Host schedule
ENV Routes__6__DownstreamHostAndPorts__0__Port 80
ENV Routes__7__DownstreamScheme http
ENV Routes__7__DownstreamHostAndPorts__0__Host workflow
ENV Routes__7__DownstreamHostAndPorts__0__Port 80
ENV Routes__8__DownstreamScheme http
ENV Routes__8__DownstreamHostAndPorts__0__Host axecore
ENV Routes__8__DownstreamHostAndPorts__0__Port 80
ENV Routes__9__DownstreamScheme http
ENV Routes__9__DownstreamHostAndPorts__0__Host axetaskmanagement
ENV Routes__9__DownstreamHostAndPorts__0__Port 80
ENV Routes__10__DownstreamScheme http
ENV Routes__10__DownstreamHostAndPorts__0__Host axereport
ENV Routes__10__DownstreamHostAndPorts__0__Port 80
ENV Routes__11__DownstreamScheme http
ENV Routes__11__DownstreamHostAndPorts__0__Host axepayment
ENV Routes__11__DownstreamHostAndPorts__0__Port 80
ENV Routes__12__DownstreamScheme ws
ENV Routes__12__ReRouteIsCaseSensitive false
ENV Routes__12__DownstreamHostAndPorts__0__Host axepayment
ENV Routes__12__DownstreamHostAndPorts__0__Port 80
ENV Routes__13__DownstreamScheme http
ENV Routes__13__DownstreamHostAndPorts__0__Host axedistributionjob
ENV Routes__13__DownstreamHostAndPorts__0__Port 80

# Copy 
WORKDIR /app
COPY ./bin/Release/net6.0/publish ./

ENTRYPOINT ["./Ce.Gateway.Api", "--urls", "http://0.0.0.0:80"]