import { useEffect, useMemo, useState } from "react";
import { useRef } from "react";
import { useNavigate } from "react-router-dom";
import { useLoading } from "../../contexts/LoadingContext";
import Button from "../../components/Button";
import MarkdownEditor from "../../components/MarkdownEditor";
import MarkdownRenderer from "../../components/MarkdownRenderer";
import { apiRequest, uploadFile } from "../../utils/apiFetcher";
import { isAdmin } from "../../utils/authService";
import { FaEdit, FaTrash } from "react-icons/fa";
import { ContentsListBox, ContentsListTile } from "../../components/ContentsList";

const EditPosts = () => {
    const navigate = useNavigate();
    const { startLoading, stopLoading } = useLoading();
    const isAdminUser = useMemo(() => isAdmin(), []);

    const [posts, setPosts] = useState([]);
    const [postId, setPostId] = useState(null);
    const [title, setTitle] = useState("");
    const [markdownBody, setMarkdownBody] = useState("");
    const [editionId, setEditionId] = useState(null);
    const [editingPost, setEditingPost] = useState(null);

    const handleBack = () => {
        navigate("/admin");
    };

    const savePost = async (text) => {
        startLoading();

        if (!title || title.trim() === "" || title.length < 3) {
            alert("Podaj tytuł posta, który ma ponad 2 znaki");
            return;
        }

        if (editingPost) {
            const apiLink = `/api/admin/post/${postId}`;
            await apiRequest(
                apiLink,
                { title: title, markdownBody: text, editionId: editionId },
                "PUT",
                navigate
            );

            setPosts(prev =>
                prev.map(p =>
                    p.id === postId
                        ? {
                            ...p,
                            title: title,
                            markdownBody: text,
                            editionId: editionId
                        }
                        : p
                )
            );
            
            setEditingPost(null);
            setTitle("");
            setMarkdownBody("");
            return;
        }

        const newPost = {
            title,
            text,
        };
        
        const data = await apiRequest(
            "/api/admin/post",
            { title: title, markdownBody: text, editionId: editionId},
            "POST",
            navigate
        );        

        await new Promise((resolve) => setTimeout(resolve, 500));
        
        if (data) {
            setPosts(prev => [...prev, data]);
            navigate("/admin/posts");
        }

        setTitle("");
        setMarkdownBody("");
        stopLoading();
    };

    const EditPost = (post) => {
        setEditingPost(
            editingPost === post ? null : post,
        );
        setPostId(
            postId === post.id ? null : post.id,
        );
        setTitle(
            title === post.title ? "" : post.title,
        );
        setMarkdownBody(
            markdownBody === post.markdownBody ? "" : post.markdownBody
        );
    };


    const DeletePost = async (post) => {
        const confirmed = window.confirm(
            `Czy na pewno chcesz usunąć post ${post.title}?`
        );

        if (!confirmed) return;
        
        const apiLink = `/api/admin/post/${post.id}`;
        const data = await apiRequest(apiLink, {}, "DELETE", navigate);
        setPosts(prev =>
            prev.filter(p => p.id !== post.id)
        );
    };

    useEffect(() => {
        if (!isAdminUser) {
            navigate("/admin/login");
            return;
        }

        const getData = async () => {
            const data = await apiRequest("/api/admin/post", null, "GET", navigate);
            const edition = false ;//await apiRequest("/api/edition", null, "GET", navigate);

            if (!edition){
                setEditionId("efc1b74d-24f3-48c5-8c74-d30cb0ffd1f8");
            }

            if (!data || data.length === 0) {
                navigate("/admin/posts");
                return;
            }

            setEditionId(edition);
            setPosts(data);
        };

        if (isAdminUser) getData();
    }, [navigate]);

    return (
        <div className="container">
            {isAdminUser && (
                <><div className="container-near">
                        <div className="container">
                            {editingPost === null && <h1>Dodaj Post {title}</h1> }
                            {editingPost !== null && <h1>Edytuj Post {title}</h1> }
                            <div>
                                <Button text={"Wróć do panelu"} onClick={handleBack} />
                                <input
                                                type="text"
                                                placeholder="Tytuł posta"
                                                value={title}
                                                onChange={(e) => setTitle(e.target.value)}
                                                required
                                            />
                                <MarkdownEditor key={markdownBody} initialValue={markdownBody} onChange={setMarkdownBody} onSave={(text) => {savePost(text); setEditingPost(null); setMarkdownBody(""); setTitle("")}} />
                            </div>
                        </div>
                        <div className="container">
                            <h1>Posty z Edycji</h1>
                            <ContentsListBox>
                                    {posts.map((item, idx) => (
                                        <ContentsListTile key={item.id}>
                                            <div style={{ display: "flex", flexDirection: "column" }}>
                                                <h3>Tytuł: {item.title}</h3>
                                                <h6>Data: {new Date(item.createdAt).toLocaleString("pl-PL", {year: "numeric", month: "long", day: "numeric", hour: "2-digit", minute: "2-digit"})}</h6>
                                                <hr style={{ border: "none", height: "2px", backgroundColor: "#054e0b", margin: "3px 0" }}/>
                                            </div>
                                            <MarkdownRenderer key={idx} content={item.markdownBody} />
                                            <hr style={{ border: "none", height: "2px", backgroundColor: "#054e0b", margin: "3px 0" }}/>
                                            <h6>Edycja:  | Edytowany???</h6>
                                            <div style={{display: "flex", gap: "4px", marginLeft: "auto",}}>
                                                <Button text={<FaEdit />} onClick={() => EditPost(item)} />
                                                <Button text={<FaTrash />} onClick={() => DeletePost(item)} />
                                            </div>
                                                
                                        </ContentsListTile>
                                    ))}
                                </ContentsListBox>
                        </div>
                    </div>
                    
                </>
            )}
            
        </div>
    );
};

export default EditPosts;
