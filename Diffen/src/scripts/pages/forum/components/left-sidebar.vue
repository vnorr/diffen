<template>
    <div class="col-lg-3 d-none d-lg-block" v-if="showLeftSideBar">
        <div class="card card-profile mb-4">
            <div class="card-header" style="background-image: url(bg.jpg);"></div>
            <div class="card-body text-center">
                <a href="/profil">
                    <img class="card-profile-img" :src="user.avatar">
                </a>
                <h6 class="card-title">
                    <a class="text-inherit" href="/profil">{{ user.nickName }}</a>
                </h6>
                <p class="mb-4">{{ user.bio }}</p>
                <ul class="card-menu">
                    <li class="card-menu-item">
                        Inlägg
                        <h6 class="my-0">{{ user.numberOfPosts }}</h6>
                    </li>
                    <li class="card-menu-item">
                        Karma
                        <h6 class="my-0">{{ user.karma }}</h6>
                    </li>
                </ul>
            </div>
        </div>
        <div class="card mb-4 mb-4">
            <div class="card-body">
                <h6 class="mb-3">Om</h6>
                <ul class="list-unstyled list-spaced mb-0">
                    <li><span class="text-muted icon icon-thumbs-up mr-3"></span>{{ user.voteStatistics.upVotes }} givna upptummar</li>
                    <li><span class="text-muted icon icon-thumbs-down mr-3"></span>{{ user.voteStatistics.downVotes }} givna nertummar</li>
                    <template v-if="user.savedPostsIds">
                        <li><span class="text-muted icon icon-bookmark mr-3"></span>{{ user.savedPostsIds.length }} sparade inlägg</li>
                    </template>
                    <template v-if="user.favoritePlayer">
                        <li><span class="text-muted icon icon-heart mr-3"></span>{{ user.favoritePlayer.fullName }}</li>
                    </template>
                    <template v-if="user.region">
                        <li class="text-uppercase"><span class="text-muted icon icon-globe mr-3"></span>DIF {{ user.region }}</li>
                    </template>
                </ul>
            </div>
        </div>
        <div class="card mb-4">
            <div class="card-body">
                <h6 class="mb-3">Länktipstoppen</h6>
                <ul class="list-unstyled list-spaced mb-0" v-if="urlTips.length > 0">
                    <li class="ellipsis" v-for="tip in urlTips" :key="tip.href + tip.clicks">
                        <span class="text-muted icon" :class="icon(tip.href)"></span>
                        <small class="ml-1"><span class="badge badge-secondary">{{ tip.clicks }}</span></small>
                        <url :tip="tip" class="ml-1" />
                    </li>
                </ul>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
import Vue from 'vue'
import { Component } from 'vue-property-decorator'
import { Getter, Action, Mutation, State, namespace } from 'vuex-class'

import { PageViewModel } from '../../../model/common'
import { UrlTip } from '../../../model/forum'

const ModuleGetter = namespace('forum', Getter)
const ModuleAction = namespace('forum', Action)
const ModuleMutation = namespace('forum', Mutation)

import { GET_URLTIP_TOPLIST, GET_SHOW_LEFT_SIDEBAR, SET_SHOW_LEFT_SIDEBAR, FETCH_URLTIP_TOPLIST } from '../../../modules/forum/types'

import Url from '../../../components/url.vue'

@Component({
    components: {
        Url
    }
})
export default class LeftSidebar extends Vue {
    @State(state => state.vm) vm: PageViewModel
    @ModuleGetter(GET_URLTIP_TOPLIST) urlTips: UrlTip[]
    @ModuleGetter(GET_SHOW_LEFT_SIDEBAR) showLeftSideBar: boolean
    @ModuleAction(FETCH_URLTIP_TOPLIST) loadUrlTipTopList: () => Promise<void>
    @ModuleMutation(SET_SHOW_LEFT_SIDEBAR) setShowLeftSideBar: (payload: { value: boolean }) => void

    mounted() {
        this.setShowLeftSideBar({ value: !this.vm.loggedInUser.filter.hideLeftMenu })
        this.loadUrlTipTopList()
    }

    get user() {
        return this.vm.loggedInUser
    }

    icon(href: string): string {
        if (href.includes('facebook'))
            return 'icon-facebook'
        if (href.includes('instagram'))
            return 'icon-instagram'
        if (href.includes('twitter'))
            return 'icon-twitter'
        if (href.includes('youtube'))
            return 'icon-youtube'
        if (href.includes('spotify'))
            return 'icon-spotify'
        if (href.includes('soundcloud'))
            return 'icon-soundcloud'
        if (href.includes('dif'))
            return 'icon-heart'
        if (href.includes('aik.se') || href.includes('aikfotboll.se'))
            return 'icon-trash'
        return 'icon-link'
    }
}
</script>
