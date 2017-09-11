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
}

// RunServer ...
func RunServer() {
	messages := make(chan Message)
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
		go decodeJSON(conn, messages)
	}
}

const endMess byte = 254
const separateMess byte = 253

func decodeJSON(c net.Conn, messChanell chan Message) {
	defer c.Close()
	dec := bufio.NewReader(c)
	var msg Message
	var curClient uint32 = 0
	for {
		data, err := dec.ReadBytes(endMess)
		if err != nil {
			fmt.Println("read" + err.Error())
			return
		}
		fmt.Println("------------------------------")
		msg.Author.ID = binary.BigEndian.Uint32(data[0:4])
		if curClient == 0 {
			curClient = msg.Author.ID
			go writeToClient(c, messChanell, curClient)
			continue
		}
		var index int = 4
		for data[index] != separateMess {
			index++
		}
		msg.Author.Name = string(data[4:index])
		index++
		startBody := index
		for data[index] != endMess {
			index++
		}
		msg.Body = string(data[startBody:index])
		messChanell <- msg
		fmt.Printf("ID: %v, Name: %v\nBody : %v\nLen: %v\n", msg.Author.ID, msg.Author.Name, msg.Body, index)
	}
}
func writeToClient(c net.Conn, messChanell <-chan Message, curClient uint32) {
	data := make([]byte, 4	, 512)
	select {
	case mess := <-messChanell:
		if mess.Author.ID != curClient {
			binary.BigEndian.PutUint32(data[0:4], mess.Author.ID)
			data = append(data, []byte(mess.Author.Name)...)
			fmt.Println("name: ", mess.Author.Name)
			fmt.Println("name[]: ", []byte(mess.Author.Name))
			data = append(data, separateMess)
			data = append(data, []byte(mess.Body)...)
			data = append(data, endMess)
			fmt.Println("mess: ", data)
			c.Write(data)
		}

	}
}
func main() {
	RunServer()
	// var input string
	// fmt.Scanln(&input)
}