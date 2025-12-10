#!/bin/sh
RETVAL=0
YIQIDONG_HOME=$(cd `dirname $0`;pwd)

echo YIQIDONG_HOME: $YIQIDONG_HOME
run()
{
    MODE=$2
    if [ $MODE = "chroot" ];
    then
        export IS_CHROOT=1
    fi

    if [ -f $YIQIDONG_HOME/YiQiDong ];
    then
        chmod +x $YIQIDONG_HOME/YiQiDong
        export DOTNET_EnableWriteXorExecute=0
        sleep 1s
        $YIQIDONG_HOME/YiQiDong -service
    else
        echo "File[$YIQIDONG_HOME/YiQiDong] not found!"
    fi
}
chroot_umount()
{
    CHROOT_HOME=$2
    if [ -z $CHROOT_HOME ];
    then        
        echo "parameter [CHROOT_HOME] is missing"
        exit 127
    fi
    if [ ! -d $CHROOT_HOME ];
    then
        echo "Error: CHROOT_HOME [$CHROOT_HOME] not found."
        exit 127
    fi
    umount $CHROOT_HOME/proc
    umount $CHROOT_HOME/sys
    if [ $# -ge 3 ] ;
    then
        index="+"
        for i in "$@";
        do
            if [ ${#index} -ge 3 ] ;
            then
                if [ -d $i ];
                then
                    if [ -d $CHROOT_HOME$i ];
                    then
                        umount $CHROOT_HOME$i
                    fi
                fi
            fi
            index=$index+
        done
    fi
}
chroot_mount()
{
    CHROOT_HOME=$2
    if [ -z $CHROOT_HOME ];
    then        
        echo "parameter [CHROOT_HOME] is missing"
        exit 127
    fi
    if [ ! -d $CHROOT_HOME ];
    then
        echo "Error: CHROOT_HOME [$CHROOT_HOME] not found."
        exit 127
    fi
    mount -t proc /proc $CHROOT_HOME/proc
    mount -t sysfs /sys $CHROOT_HOME/sys
    
    if [ $# -ge 3 ] ;
    then
        index="+"
        for i in "$@";
        do
            if [ ${#index} -ge 3 ] ;
            then
                echo "Mounting [$i]..."
                if [ ! -d $i ];
                then
                    mkdir -p $i
                fi
                if [ ! -d $CHROOT_HOME$i ];
                then
                    mkdir -p $CHROOT_HOME$i
                fi
                mount -o bind $i $CHROOT_HOME$i
            fi
            index=$index+
        done
            
    fi
}
chroot_run()
{
    CHROOT_HOME=$2
    if [ -z $CHROOT_HOME ];
    then        
        echo "Error: Parameter [CHROOT_HOME] is missing"
        exit 127
    fi
    if [ ! -d $CHROOT_HOME ];
    then
        echo "Error: CHROOT_HOME [$CHROOT_HOME] not found."
        exit 127
    fi
    chroot_umount $*
    chroot_mount $*
    chroot $CHROOT_HOME /usr/share/YiQiDong/YiQiDong.sh run chroot
}
start()
{
    if [ -f $YIQIDONG_HOME/YiQiDong ];
    then
        echo "Starting YiQiDong"
        chmod +x $YIQIDONG_HOME/YiQiDong
        export DOTNET_EnableWriteXorExecute=0
        rm -f $YIQIDONG_HOME/YiQiDong.env.sh
        $YIQIDONG_HOME/YiQiDong -prepare > $YIQIDONG_HOME/YiQiDong.env.sh
        . $YIQIDONG_HOME/YiQiDong.env.sh
        rm -f $YIQIDONG_HOME/YiQiDong.env.sh
        $YIQIDONG_HOME/YiQiDong -service &
        RETVAL=0
        echo " OK"
        return $RETVAL
    fi
}
stop()
{
    echo "Stopping YiQiDong"
    RETVAL=$?
    ps -ef | grep 'YiQiDong -service' | grep -v PID | awk '{print $2}'|xargs kill
    echo " OK"
    return $RETVAL
}

case "$1" in
 run)
        run $*
        ;;
 start)
        start
        ;;
 stop)
        stop
        ;;
 restart)
         echo "Restaring YiQiDong"
         $0 stop
         sleep 1
         $0 start
         ;;
 chroot_run)
        chroot_run $*
        ;;
 chroot_mount)
        chroot_mount $*
        ;;
 chroot_umount)
        chroot_umount $*
        ;;
 status)
        ps -ef | grep '$YIQIDONG_HOME/YiQiDong -service' >>null
        if [ $? -ne 0 ]
        then
         echo "YiQiDong stoped"
        else
         ps -ef | grep '$YIQIDONG_HOME/YiQiDong -service' | awk '{print "YiQiDong pid: "$2}'
         echo "YiQiDong is runing....."
        fi
        ;;
 *)
        echo $"Usage: $0 {run|start|stop|restart|status|chroot_run|chroot_mount|chroot_umount}"
        exit 1
        ;;
esac
exit $RETVAL