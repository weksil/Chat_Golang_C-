package main

import (
	"bufio"
	"encoding/binary"
	"fmt"
	"net"
)

// Message ...
type Message struct {
	Author Client
	Body   []byte
}

// Client ...
type Client struct {
	ID   uint32 // 4 byte
	Name string
	Conn net.Conn
}
func SendToClient (client Client, mess []byte){
	_, err := client.Conn.Write(mess)
	if err != nil {
		fmt.Println("send" + err.Error())
		return
	}
}
var clients map[uint32]Client
var messages chan Message

// RunServer ...
func RunServer() {
	messages = make(chan Message)
	clients = make(map[uint32]Client) 
	listener, _ := net.Listen("tcp", ":5555")
	fmt.Println("Run server")
	for {
		conn, err := listener.Accept()
		if err != nil {
			fmt.Println("Error, cant connect")
			continue
		}
		fmt.Println("Connected")
		fmt.Print("\n")
		go listenClient(conn)
	}
}

const endMess byte = 255
const commMess byte = 200
const commGetID byte = 201
const commDisconnect byte = 202

func listenClient(c net.Conn){
	defer c.Close()
	dec := bufio.NewReader(c)
	var client Client
	var msg Message
	for{
		data, err := dec.ReadBytes(endMess)
		if err != nil{
			fmt.Println("error read, disconnect")
			delete(clients,client.ID)
			break
		}  
		switch data[0] {
			case commMess:
				msg.Body = data
				SendMessages(msg)
			case commDisconnect:
				delete(clients,client.ID)
				return
			case commGetID:
				client = createClient(c,data)
				idData := make([]byte,6) 
				binary.BigEndian.PutUint32(idData[1:5], client.ID)
				idData[0] = commGetID
				idData[5] = endMess
				SendToClient(client,idData)
				msg.Author = client
			default:
				fmt.Println("err command")
			}
		}
	}

func SendMessages(mess Message) {
	for k:= range clients{
		if mess.Author.ID == k {
			continue
		}
		SendToClient(clients[k],mess.Body)
	}
}

var numID uint32 = 0

func createClient(connect net.Conn, data []byte) Client {
	var res Client
	numID ++ 
	res.Conn = connect
	res.ID = numID
	res.Name = string(data[1:])
	clients[numID] = res
	return res
}

func main() {
	go RunServer()
	for{}
}