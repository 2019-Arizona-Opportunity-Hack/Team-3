import React, { useEffect, useState } from "react";
import EventTable from "../components/EventTable";
import useRequest from "../hooks/useRequest";
import config from "../config.json";

export default function UpcomingEvents() {
  const { get } = useRequest();
  const [events, setEvents] = useState([]);

  useEffect(() => {
    async function getEvents() {
      let response = await get(config.GET_UPCOMING_EVENTS_GET_URL, {});
      if (response.success) {
        setEvents(response.data);
      } else {
      }
    }
    getEvents();
    return () => {};
  }, []);

  return (
    <div>
      <EventTable events={events} />
    </div>
  );
}