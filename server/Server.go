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
	Body   string
}

// Client ...
type Client struct {
	ID   uint32 // 4 byte
	Name string
	Conn net.Conn
}

// RunServer ...
func RunServer() {
	messages := make(chan Message)
	var clients map[uint32]Client = make(map[uint32]Client) 
	listener, _ := net.Listen("tcp", ":5555")
	fmt.Println("Run server")
	go writeToClient(messages,clients)	
	for {
		conn, err := listener.Accept()
		if err != nil {
			fmt.Println("Error, cant connect")
			continue
		}
		fmt.Println("Connected")
		fmt.Print("\n")
		go decodeJSON(conn, messages,clients)
	}
}

const endMess byte = 254
const separateMess byte = 253

func decodeJSON(c net.Conn, messChanell chan<- Message,clients map[uint32]Client) {
	defer c.Close()
	dec := bufio.NewReader(c)
	var msg Message
	var curClient uint32 = 0
	var index int
	for {
		data, err := dec.ReadBytes(endMess)
		if err != nil {
			delete(clients,curClient)
			fmt.Println("read" + err.Error())
			return
		}
		
		msg.Author.ID = binary.BigEndian.Uint32(data[0:4])
		index = 4
		for data[index] != separateMess {
			index++
		}
		msg.Author.Name = string(data[4:index])
		if curClient == 0 {
			curClient = msg.Author.ID
			var client Client
			client.Conn = c
			client.ID = curClient
			client.Name = msg.Author.Name
			clients[client.ID] = client
			continue
		}
		index++
		startBody := index
		for data[index] != endMess {
			index++
		}
		msg.Body = string(data[startBody:index])
		fmt.Printf("ID: %v, Name: %v\nBody : %v\nLen: %v\n", msg.Author.ID, msg.Author.Name, msg.Body, index)
		messChanell <- msg
	}
}
func writeToClient(messChanell <-chan Message,clients map[uint32]Client) {
	for {
		select {
		case mess := <-messChanell:
			data := make([]byte, 4	, 512)
			binary.BigEndian.PutUint32(data[0:4], mess.Author.ID)
			data = append(data, []byte(mess.Author.Name)...)
			data = append(data, separateMess)
			data = append(data, []byte(mess.Body)...)
			data = append(data, endMess)
			for k:= range clients{
				if mess.Author.ID == k {
					continue
				}
				_, err := clients[k].Conn.Write(data)
				if err != nil {
					fmt.Println("read" + err.Error())
					return
				}
			}
		}
	}
}
func main() {
	go RunServer()
	for{}
}