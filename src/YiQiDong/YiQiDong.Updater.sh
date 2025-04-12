#!/bin/sh
launchctl stop YiQiDong
sleep 5
launchctl start YiQiDong

rm ~/Library/LaunchAgents/YiQiDong.Updater.plist
launchctl remove YiQiDong.Updater
