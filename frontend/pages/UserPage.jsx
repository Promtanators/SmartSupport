import { Input, Button, Typography } from "antd";
import { SendOutlined } from "@ant-design/icons";
import { useState } from "react";

export default function UserPage({ handleClick }) {
  const [message, setMessage] = useState("");

  return (
    <>
      <div
        style={{
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          height: "100vh",
        }}
      >
        <div style={{ display: "flex", flexDirection: "column" }}>
          <Typography.Text italic style={{ fontSize: 20 }}>
            Сообщите нам о Вашей проблеме!
          </Typography.Text>

          <div style={{ display: "flex" }}>
            <Input.TextArea
              placeholder="Какая у Вас проблема?"
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              style={{ width: 600, marginRight: 8 }}
              autoSize={{ minRows: 1, maxRows: 4 }}
            />
            <Button
              type="primary"
              icon={<SendOutlined />}
              onClick={handleClick}
            />
          </div>
        </div>
      </div>
    </>
  );
}
